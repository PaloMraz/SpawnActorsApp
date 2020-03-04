using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using PrefabActor.Interfaces;
using System.Fabric;

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

    // In order to achieve successfull reminder registration, we use timer to try it several times
    // based on parameters defined here.
    private IActorTimer _reminderRegistrationTimer;
    private int _remiderRegistrationRetryCount = 0;
    private const int ReminderRegistrationRetryIntervalMilliseconds = 10 * 1000; 
    private const int MaxRemiderRegistrationRetryCount = 6;

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
      // Start timer that will try to register the reminder every 10 seconds until it succeeds or 
      // the specified number of retries is made.
      this._reminderRegistrationTimer = this.RegisterTimer(
        asyncCallback: state => this.RegisterWakeUpReminderTimerCallbackAsync(),
        state: null,
        dueTime: TimeSpan.FromMilliseconds(ReminderRegistrationRetryIntervalMilliseconds),
        period: TimeSpan.FromMilliseconds(ReminderRegistrationRetryIntervalMilliseconds));

      return Task.CompletedTask;
    }


    protected override Task OnDeactivateAsync()
    {
      if (this._reminderRegistrationTimer != null)
      {
        this.UnregisterTimer(this._reminderRegistrationTimer);
      }

      return Task.CompletedTask;
    }


    Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
      ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}{this.Id} received {reminderName} on {DateTimeOffset.Now:O}");
      return Task.CompletedTask;
    }


    private async Task RegisterWakeUpReminderTimerCallbackAsync()
    {
      try
      {
        this._remiderRegistrationRetryCount += 1;

        await this.RegisterReminderAsync(reminderName: "WakeUpActor", state: null, dueTime: TimeSpan.FromSeconds(30), period: TimeSpan.FromSeconds(10));

        // Reminder registration succeeded so we can now delete the timer.
        this.DeleteReminderRegistrationTimer();

        ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}{this.Id} Reminder successfully registered");
      }
      catch (FabricTransientException ex) // should be really ReminderLoadInProgressException, but it is internal in Microsoft.ServiceFabric.Actors.dll
      {
        // Reminder registration failed - if we have reached the maximum number of retries, we will destroy the timer.
        if (this._remiderRegistrationRetryCount >= MaxRemiderRegistrationRetryCount)
        {
          this.DeleteReminderRegistrationTimer();
        }
        ActorEventSource.Current.ActorMessage(this, $"{MessagePrefix}{this.Id} RegisterReminderAsync failed: {ex.GetBaseException().Message}");
      }
    }


    private void DeleteReminderRegistrationTimer()
    {
      this.UnregisterTimer(this._reminderRegistrationTimer);
      this._reminderRegistrationTimer = null;
    }
  }
}
