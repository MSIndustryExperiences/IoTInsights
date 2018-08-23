docker build --rm -t flattenandpost:1.1 .
docker login ....
docker tag flattenandpost ......azurecr.io/smsprocess/flattenandpost
docker push .....azurecr.io/smsprocess/flattenandpost:1.1