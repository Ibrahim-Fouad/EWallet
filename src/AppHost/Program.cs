var builder = DistributedApplication.CreateBuilder(args);

var sqlServerPassword =
    builder.AddParameter("sqlserver-password", "H-6H+hRzHWQrr-nZ4.3(mT", secret: true);

var sqlServer = builder.AddSqlServer("sqlserver", port: 50607, password: sqlServerPassword)
    .WithContainerName("ewallet-sqlserver")
    .WithDataVolume("ewallet-sqlserver-data")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase("EWallet");

var redis = builder.AddRedis("redis")
    .WithContainerName("ewallet-redis")
    .WithDataVolume("ewallet-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithContainerName("ewallet-rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume("ewallet-rabbitmq-data")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.EWallet_API>("api")
    .WithReference(db, connectionName: "sqlserver").WaitFor(db)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

builder.AddProject<Projects.EWallet_Gateway>("gateway")
    .WithReference(api).WaitFor(api);

builder.Build().Run();
