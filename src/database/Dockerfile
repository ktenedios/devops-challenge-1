FROM mcr.microsoft.com/mssql/server:2019-latest

# Environment variables are expected to be overwritten by process that builds image
ENV ACCEPT_EULA="Y" \
    SA_PASSWORD="_" \
    DB_APP_USERNAME="_" \
    DB_APP_PASSWORD="_"

WORKDIR scripts
COPY setup*.* scripts/

RUN ./scripts/setup-db.sh $env:SA_PASSWORD $env:DB_APP_USERNAME $env:DB_APP_PASSWORD
