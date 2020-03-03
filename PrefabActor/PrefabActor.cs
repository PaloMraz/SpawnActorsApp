using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using PrefabActor.Interfaces;

namespace PrefabActor
{
  /// <remarks>
  /// This class represents an actor.
  /// Every ActorID maps to an instance of this class.
  /// The StatePersistence attribute determines persistence and replication of actor state:
  ///  - Persisted: State is written to disk and replicated.
  ///  - Volatile: State is kept in memory only and replicated.
  ///  - None: State is kept in memory only and not replicated.
  /// </remarks>
  [StatePersistence(StatePersistence.Persisted)]
  internal class PrefabActor : Actor, IPrefabActor, IRemindable
  {
    private const string MessagePrefix = "********* ";

    /// <summary>
    /// Initializes a new instance of PrefabActor
    /// </summary>
    /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
    /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
    public PrefabActor(ActorService actorService, ActorId actorId)
        : base(actorService, actorId)
    {
    }

    public async Task MakeSureActorIsActivatedAsync()
    {
      await Task.Yield();
    }


    protected override Task OnActivateAsync()
    {
      try
      {
        this.RegisterReminderAsync(reminderName: "WakeUpActor", state: null, dueTime: TimeSpan.FromSeconds(30), period: TimeSpan.FromSeconds(10));
        ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}Reminder successfully registered");
      }
      catch (Exception ex)
      {
        ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}RegisterReminderAsync failed: {ex.GetBaseException().Message}");
      }

      return Task.CompletedTask;
    }


    Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
      ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}{this.Id} received {reminderName} on {DateTimeOffset.Now:O}");
      return Task.CompletedTask;
    }
  }
}
