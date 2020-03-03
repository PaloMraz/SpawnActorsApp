using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using PrefabActor.Interfaces;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrefabActor
{
  public class PrefabActorService : ActorService
  {
    public PrefabActorService(
      StatefulServiceContext context,
      ActorTypeInformation actorTypeInfo,
      Func<ActorService, ActorId, ActorBase> actorFactory = null,
      Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
      IActorStateProvider stateProvider = null,
      ActorServiceSettings settings = null)
        : base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
    {
    }


    protected async override Task RunAsync(CancellationToken cancellationToken)
    {
      // According to this article:
      // https://stackoverflow.com/questions/36729732/service-fabric-spawn-actor-on-startup
      // Calling base.RunAsync should ensure aftre that the replica is initialized
      await base.RunAsync(cancellationToken);

      // Now we'll try to spawn a couple of actors with predefined string Ids hoping they will be distributed
      // between the primary service replicas.
      string[] actorIdStrings = { "Ringo", "John", "Paul", "George" };
      IEnumerable<Task> actorActivationTasks = actorIdStrings.Select(actorIdString =>
      {
        IPrefabActor actor = ActorProxy.Create<IPrefabActor>(new ActorId(actorIdString), this.Context.ServiceName);
        return actor.MakeSureActorIsActivatedAsync();
      });
      await Task.WhenAll(actorActivationTasks);
    }
  }
}
