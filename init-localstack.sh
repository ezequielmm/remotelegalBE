#This script create the lambda function and initialize LocalStack docker-compose

echo "Creating lambda file..."
cd ./src/AWS/Lambdas/UploadExhibitLambda
dotnet publish -c Debug -o Publish

echo "Zipping lambda file..."
if [ "$(expr substr $(uname -s) 1 10)" == "MINGW32_NT" ] || [ "$(expr substr $(uname -s) 1 10)" == "MINGW64_NT" ]; then
    echo "Compressing file in Windows enviroment..."
    powershell -command "& {Compress-Archive -Path '.\Publish\*.*' -Destinationpath '../../../LocalStack/publish.zip' -Force}"
else
    echo "Compressing file in non Windows enviroment..."
    zip -r ../../../LocalStack/publish.zip ./Publish/*
fi
echo "Lambda zip file succefully created!"

cd ../../../LocalStack/
echo "Ensuring to initialize a fresh instance..."
docker-compose --env-file ../../.env down

echo "Running docker-compose..."
docker-compose --env-file ../../.env up