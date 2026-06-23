# hhs-service-event-manager

```bash
dotnet nuget add source "https://nuget.pkg.github.com/tech-summus/index.json" \
    --name github \
    --username hsnsh \
    --password <GITHUB_TOKENIN> \
    --store-password-in-clear-text
```

```bash
kubectl run mongo-test \
  -n ns-hhs-uat-apps \
  --rm -it \
  --image=mongo:7 \
  --restart=Never \
  -- \
  mongosh 'mongodb://root:MoNgO_RooT_Passw0rd!@mongo-service.ns-mongo.svc.cluster.local:27017/HHS_EventManagerService_Uat?authSource=admin'
```

