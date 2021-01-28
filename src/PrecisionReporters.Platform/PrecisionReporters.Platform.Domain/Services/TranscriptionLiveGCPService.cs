using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Data.Entities;
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
    public class TranscriptionLiveGCPService : ITranscriptionLiveService
    {
        private const int ChannelCount = 1;
        private const int BytesPerSample = 2;
        private static readonly TimeSpan _streamTimeLimit = TimeSpan.FromSeconds(290);

        private readonly GcpConfiguration _gcpConfiguration;
        private ITranscriptionService _transcriptionService;
        private SpeechClient.StreamingRecognizeStream _rpcStream;
        private readonly BlockingCollection<ByteString> _audioBuffer = new BlockingCollection<ByteString>();
        private readonly LinkedList<ByteString> _processingBuffer = new LinkedList<ByteString>();
        private ValueTask<bool> _serverResponseAvailableTask;
        private TimeSpan _processingBufferStart;
        private DateTime _rpcStreamDeadline;
        private readonly SpeechClient _client;

        public TranscriptionLiveGCPService(IOptions<GcpConfiguration> gcpConfiguration, ITranscriptionService transcriptionService)
        {
            _transcriptionService = transcriptionService;
            _gcpConfiguration = gcpConfiguration.Value;
            _client = GetGCPCredentials();
        }


        public async Task<Transcription> RecognizeAsync(byte[] audioChunk, string userEmail, string depositionId, int sampleRate)
        {
            _audioBuffer.Add(ByteString.CopyFrom(audioChunk, 0, audioChunk.Length));
            return await RunAsync(userEmail, depositionId, sampleRate);
        }

        private async Task<Transcription> RunAsync(string userEmail, string depositionId, int sampleRate)
        {
            await MaybeStartStreamAsync(sampleRate);

            var transcription = ProcessResponses(sampleRate);

            if (!string.IsNullOrEmpty(transcription.Text))
            {
                await _transcriptionService.StoreTranscription(transcription, depositionId, userEmail);
            }

            await TransferAudioChunkAsync();

            return transcription;
        }

        private async Task MaybeStartStreamAsync(int sampleRate)
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
            _rpcStreamDeadline = now + _streamTimeLimit;
            _processingBufferStart = TimeSpan.Zero;
            _serverResponseAvailableTask = _rpcStream.GetResponseStream().MoveNextAsync();
            await _rpcStream.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    Config = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = sampleRate,
                        AudioChannelCount = ChannelCount,
                        LanguageCode = "en-US",
                        MaxAlternatives = 1,
                        EnableAutomaticPunctuation = true,
                        Model = "phone_call",
                        UseEnhanced = true,
                        EnableWordTimeOffsets = true
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

        private Transcription ProcessResponses(int sampleRate)
        {
            var transcription = new Transcription();

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
                    transcription.Text = finalResult.Alternatives[0].Transcript;
                    Trace.WriteLine($"Transcript: {transcription.Text}");

                    TimeSpan resultEndTime = finalResult.ResultEndTime.ToTimeSpan();

                    // Rather than explicitly iterate over the list, we just always deal with the first
                    // element, either removing it or stopping.
                    int removed = 0;
                    while (_processingBuffer.First != null)
                    {
                        var BytesPerSecond = sampleRate * ChannelCount * BytesPerSample;
                        var sampleDuration = TimeSpan.FromSeconds(_processingBuffer.First.Value.Length / (double)(BytesPerSecond));
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

                    transcription.TranscriptDateTime = DateTime.UtcNow;
                }
            }

            return transcription;
        }

        private async Task TransferAudioChunkAsync()
        {
            // This will block - but only for ~100ms, unless something's really broken.
            var chunk = _audioBuffer.Take();
            _processingBuffer.AddLast(chunk);
            await WriteAudioChunk(chunk);
        }

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
