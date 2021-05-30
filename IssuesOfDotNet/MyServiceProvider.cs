using System;
using System.Net.Http;

namespace IssuesOfDotNet
{
    public sealed class MyServiceProvider : IServiceProvider
    {
        private readonly MyAutoCompletionService _service;

        public MyServiceProvider(HttpClient client)
        {
            _service = new MyAutoCompletionService(client);
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(MyAutoCompletionService))
            {
                return _service;
            }

            return null;
        }
    }
}
