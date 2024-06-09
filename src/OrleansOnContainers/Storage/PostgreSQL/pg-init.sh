#!/bin/bash
set -e

# Check positional parameters and environment for a database user.
if [[ ! -z $1 ]]; then
    user=$1
elif [[ ! -z $POSTGRES_USER ]]; then
    user=$POSTGRES_USER
else
    echo "No user provided."
    exit 1
fi

# Check positional parameters and environment for a directory for sql scripts.
if [[ ! -z $2 ]]; then
    orleansScriptsDirectory=$2
elif [[ ! -z $ORLEANS_SCRIPTS_DIR ]]; then
    orleansScriptsDirectory=$ORLEANS_SCRIPTS_DIR
else
    echo "No directory provided."
    exit 1
fi

# Assign this string to a variable for simplicities sake.
ooc="orleans_on_containers"

# Ideally these values would be retrieved from environment variables that have
# been set in the container (obviously a hard-coded password is a bad idea).
database=$ooc
username=$ooc
password=$ooc

# Create a database specifically for the orleans tables, as well as a role that
# will be used to interact with them, instead of using the postgres db and superuser.
psql -v ON_ERROR_STOP=1 --username $user \
    -c "CREATE DATABASE $database;" \
    -c "CREATE USER $username WITH PASSWORD '$password';"

# Run the scripts to add the required database artifacts.
psql -v ON_ERROR_STOP=1 --username $user --dbname $database \
    --file=${orleansScriptsDirectory}/PostgreSQL-Main.sql \
    --file=${orleansScriptsDirectory}/PostgreSQL-Clustering.sql \
    --file=${orleansScriptsDirectory}/PostgreSQL-Clustering-3.6.0.sql \
    --file=${orleansScriptsDirectory}/PostgreSQL-Clustering-3.7.0.sql

# List all of the tables added by the above scripts.
orleansTables=(
    "orleansmembershiptable"
    "orleansmembershipversiontable"
    "orleansquery")

# For each of the above tables, grant permissions to the specialised user. 
for table in ${orleansTables[@]}; do
    psql -v ON_ERROR_STOP=1 --username $user --dbname $database \
        -c "GRANT SELECT, INSERT, UPDATE, DELETE ON $table TO $username;"
done