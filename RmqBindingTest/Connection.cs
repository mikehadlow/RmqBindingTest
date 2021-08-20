using System;

namespace RmqBindingTest
{
    public static class Connection
    {
        public static string Host => GetEnv("BINDING_TEST_RMQ_HOST");
        public static string Vhost => GetEnv("BINDING_TEST_RMQ_VHOST");
        public static string User => GetEnv("BINDING_TEST_RMQ_USER");
        public static string Password => GetEnv("BINDING_TEST_RMQ_PASSWORD");
        public static string Exchange => "BindingTest";
        public static string StatsExchange => "BindingTestStats";

        static string GetEnv(string variableName)
            => Environment.GetEnvironmentVariable(variableName) 
            ?? throw new ApplicationException($"Expected environemnt variable '{variableName}', but was not found.");
    }
}
