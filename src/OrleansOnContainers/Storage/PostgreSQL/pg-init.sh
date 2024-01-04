#!/bin/bash
set -e

# Assign this string to a variable for simplicities sake.
ooc="orleans_on_containers"

# Ideally these values would be retrieved from environment variables that have
# been set in the container (obviously a hard-coded password is a bad idea).
database=$ooc
username=$ooc
password=$ooc

# Create a database specifically for the orleans tables, as well as a role that
# will be used to interact with them, instead of using the postgres db and superuser.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" \
    -c "CREATE DATABASE $database;" \
    -c "CREATE USER $username WITH PASSWORD '$password';"

# Run the scripts to add the required database artifacts.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$database" \
    --file=/scripts/PostgreSQL-Main.sql \
    --file=/scripts/PostgreSQL-Clustering.sql \
    --file=/scripts/PostgreSQL-Persistence.sql \
    --file=/scripts/PostgreSQL-Reminders.sql

# List all of the tables added by the above scripts.
orleansTables=(
    "orleansmembershiptable"
    "orleansmembershipversiontable"
    "orleansquery"
    "orleansreminderstable"
    "orleansstorage")

# For each of the above tables, grant permissions to the specialised user. 
for table in ${orleansTables[@]}; do
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$database" \
        -c "GRANT SELECT, INSERT, UPDATE, DELETE ON $table TO $username;"
done