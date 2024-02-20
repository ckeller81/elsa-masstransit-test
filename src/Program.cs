using Elsa.Extensions;
using Elsa.MassTransit.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using MassTransit;

namespace ElsaMassTransitTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddElsa(elsa => elsa
                .UseMassTransit(f => f
                    .UseRabbitMq("rabbitmq://guest:guest@localhost")
                    .AddMessageType<IMessage>()
                )
                .AddWorkflow<MyMessageWorkflow>()
                .AddWorkflow<MySimpleWorkflow>()
            );

            var app = builder.Build();

            app.MapGet("/", (IBus bus) =>
            {
                bus.Publish<IMessage>(new { Content = "... from Bus" });
                return "Message published";
            });

            app.MapGet("/simple", (IWorkflowDispatcher dispatcher) =>
            {
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest
                {
                    DefinitionId = nameof(MySimpleWorkflow)
                });
                return "Simple workflow dispatched";
            });

            app.Run();
        }
    }

    public interface IMessage
    {
        string Content { get; }
    }

    public class MyMessageWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var message = builder.WithVariable<IMessage>();
            builder.Root = new Sequence
            {
                Activities =
                {
                    new MessageReceived { MessageType = typeof(IMessage), Result = new (message), CanStartWorkflow = true },
                    new WriteLine(ctx => $"Hello world {message.Get<IMessage>(ctx)?.Content}")
                }
            };
        }
    }

    public class MySimpleWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("Hello world from simple workflow")
                }
            };
        }
    }
}
