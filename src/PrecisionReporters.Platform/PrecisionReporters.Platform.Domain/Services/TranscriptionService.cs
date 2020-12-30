using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private const int SampleRate = 44100;
        private const int ChannelCount = 1;
        private const int BytesPerSample = 2;
        private const int BytesPerSecond = SampleRate * ChannelCount * BytesPerSample;
        private static readonly TimeSpan s_streamTimeLimit = TimeSpan.FromSeconds(290);
        private readonly SpeechClient _client;
        private readonly GcpConfiguration _gcpConfiguration;

        /// <summary>
        /// Audio chunks that haven't yet been processed at all.
        /// </summary>
        private readonly BlockingCollection<ByteString> _audioBuffer = new BlockingCollection<ByteString>();

        /// <summary>
        /// Chunks that have been sent to Cloud Speech, but not yet finalized.
        /// </summary>
        private readonly LinkedList<ByteString> _processingBuffer = new LinkedList<ByteString>();

        /// <summary>
        /// The start time of the processing buffer, in relation to the start of the stream.
        /// </summary>
        private TimeSpan _processingBufferStart;

        /// <summary>
        /// The current RPC stream, if any.
        /// </summary>
        private SpeechClient.StreamingRecognizeStream _rpcStream;

        /// <summary>
        /// The deadline for when we should stop the current stream.
        /// </summary>
        private DateTime _rpcStreamDeadline;

        /// <summary>
        /// The task indicating when the next response is ready, or when we've
        /// reached the end of the stream. (The task will complete in either case, with a result
        /// of True if it's moved to another response, or False at the end of the stream.)
        /// </summary>
        private ValueTask<bool> _serverResponseAvailableTask;

        public TranscriptionService(IOptions<GcpConfiguration> gcpConfiguration)
        {
            _gcpConfiguration = gcpConfiguration.Value;
            _client = GetGCPCredentials();
        }

        public async Task<string> RecognizeAsync(byte[] audioChunk)
        {
            _audioBuffer.Add(ByteString.CopyFrom(audioChunk, 0, audioChunk.Length));
            return await RunAsync();
        }

        private async Task<string> RunAsync()
        {
            await MaybeStartStreamAsync();

            var transcript = ProcessResponses();
            await TransferAudioChunkAsync();

            return transcript;
        }

        /// <summary>
        /// Starts a new RPC streaming call if necessary. This will be if either it's the first call
        /// (so we don't have a current request) or if the current request will time out soon.
        /// In the latter case, after starting the new request, we copy any chunks we'd already sent
        /// in the previous request which hadn't been included in a "final result".
        /// </summary>
        private async Task MaybeStartStreamAsync()
        {
            var now = DateTime.UtcNow;
            if (_rpcStream != null && now >= _rpcStreamDeadline)
            {
                Trace.WriteLine($"Closing stream before it times out");
                await _rpcStream.WriteCompleteAsync();
                _rpcStream.GrpcCall.Dispose();
                _rpcStream = null;
            }

            // If we have a valid stream at this point, we're fine.
            if (_rpcStream != null)
            {
                return;
            }

            // We need to create a new stream, either because we're just starting or because we've just closed the previous one.
            _rpcStream = _client.StreamingRecognize();
            _rpcStreamDeadline = now + s_streamTimeLimit;
            _processingBufferStart = TimeSpan.Zero;
            _serverResponseAvailableTask = _rpcStream.GetResponseStream().MoveNextAsync();
            await _rpcStream.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    Config = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = SampleRate,
                        AudioChannelCount = ChannelCount,
                        LanguageCode = "en-US",
                        MaxAlternatives = 1,
                        EnableAutomaticPunctuation = true,
                        UseEnhanced = true
                    },
                    InterimResults = false,
                }
            });

            Trace.WriteLine($"Writing {_processingBuffer.Count} chunks into the new stream.");
            foreach (var chunk in _processingBuffer)
            {
                await WriteAudioChunk(chunk);
            }
        }

        /// <summary>
        /// Processes responses received so far from the server,
        /// returning whether "exit" or "quit" have been heard.
        /// </summary>
        private string ProcessResponses()
        {
            var transcript = string.Empty;

            while (_serverResponseAvailableTask.IsCompleted && _serverResponseAvailableTask.Result)
            {
                var response = _rpcStream.GetResponseStream().Current;
                _serverResponseAvailableTask = _rpcStream.GetResponseStream().MoveNextAsync();
                // Uncomment this to see the details of interim results.
                Trace.WriteLine($"Response: {response}");

                // See if one of the results is a "final result". If so, we trim our
                // processing buffer.
                var finalResult = response.Results.FirstOrDefault(r => r.IsFinal);
                if (finalResult != null)
                {
                    transcript = finalResult.Alternatives[0].Transcript;
                    Trace.WriteLine($"Transcript: {transcript}");

                    TimeSpan resultEndTime = finalResult.ResultEndTime.ToTimeSpan();

                    // Rather than explicitly iterate over the list, we just always deal with the first
                    // element, either removing it or stopping.
                    int removed = 0;
                    while (_processingBuffer.First != null)
                    {
                        var sampleDuration = TimeSpan.FromSeconds(_processingBuffer.First.Value.Length / (double)BytesPerSecond);
                        var sampleEnd = _processingBufferStart + sampleDuration;

                        // If the first sample in the buffer ends after the result ended, stop.
                        // Note that part of the sample might have been included in the result, but the samples
                        // are short enough that this shouldn't cause problems.
                        if (sampleEnd > resultEndTime)
                        {
                            break;
                        }
                        _processingBufferStart = sampleEnd;
                        _processingBuffer.RemoveFirst();
                        removed++;
                    }
                }
            }
            return transcript;
        }

        /// <summary>
        /// Takes a single sample chunk from the microphone buffer, keeps a local copy
        /// (in case we need to send it again in a new request) and sends it to the server.
        /// </summary>
        /// <returns></returns>
        private async Task TransferAudioChunkAsync()
        {
            // This will block - but only for ~100ms, unless something's really broken.
            var chunk = _audioBuffer.Take();
            _processingBuffer.AddLast(chunk);
            await WriteAudioChunk(chunk);
        }

        /// <summary>
        /// Writes a single chunk to the RPC stream.
        /// </summary>
        private Task WriteAudioChunk(ByteString chunk) =>
            _rpcStream.WriteAsync(new StreamingRecognizeRequest { AudioContent = chunk });

        private SpeechClient GetGCPCredentials()
        {            
            var gcpCredentials = JsonConvert.SerializeObject(_gcpConfiguration);
            var speechClient = new SpeechClientBuilder
            {
                JsonCredentials = gcpCredentials
            };

            return speechClient.Build();
        }
    }
}