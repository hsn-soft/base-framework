dotnet dev-certs https -v -ep etc/dev-cert/localhost.pfx -p e8202f07-66e5-4619-be07-72ba76fde97f -t

#openssl pkcs12 -in localhost.pfx -clcerts -nokeys -out localhost.crt
#openssl pkcs12 -in localhost.pfx -nocerts -nodes -out localhost.key

dotnet dev-certs https --trust

#dotnet dev-certs https -ep ${HOME}/.aspnet/https/localhost.pfx -p e8202f07-66e5-4619-be07-72ba76fde97f -t
#dotnet dev-certs https --trust




