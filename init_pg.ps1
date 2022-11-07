docker stop postgresql_database

docker run --name postgresql_database `
    --memory 512M `
    --rm `
    -d `
    -e POSTGRES_USER=pguser `
    -e POSTGRES_PASSWORD=egm7DfeK `
    -e POSTGRESQL_DATABASE=identity `
    -p 5432:5432 `
    docker.io/library/postgres:latest
    #-v "$((pwd).Path))/data":/var/lib/postgresql/data `
    