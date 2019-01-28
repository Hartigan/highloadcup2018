docker build -t hlcup2018 .
docker run -v ~/repository/bigdata/data:/tmp/data -p 8080:80 --memory=1850m --memory-swap=0b -t hlcup2018