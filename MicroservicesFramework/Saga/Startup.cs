using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hetacode.Microless.Abstractions.MessageBus;
using Hetacode.Microless.Extensions;
using Hetacode.Microless.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saga.Sagas;
using Saga.StateMachine;
using Saga.StateMachine.Managers;

namespace Saga
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMicroless();
            services.AddMessageBus(config =>
            {
                config.Provider = new RabbitMQProvider("192.168.8.140", "guest", "guest", "saga");
            });

            services.AddSingleton<StepsManager>();
            services.AddSingleton<StatesBuilder>();

            services.AddTransient<TestMessagesSaga>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
            app.UseMessageBus((functions, subscribe) =>
            {
                subscribe.AddReceiver("Saga", (message, headers) =>
                {
                    var steps = app.ApplicationServices.GetService<StepsManager>();
                    var step = steps.Get(message);
                    var bus = app.ApplicationServices.GetService<IBusSubscriptions>();
                    step(new Hetacode.Microless.Context(bus), message);
                });
            });

            _ = app.ApplicationServices.GetService<TestMessagesSaga>();
        }
    }
}
