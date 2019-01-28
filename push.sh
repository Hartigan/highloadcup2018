docker build -t hlcup2018 .
docker tag hlcup2018 stor.highloadcup.ru/accounts/badger_builder
docker push stor.highloadcup.ru/accounts/badger_builder