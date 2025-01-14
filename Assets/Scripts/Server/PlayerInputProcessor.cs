using Priority_Queue;
using System.Collections.Generic;

// Simple structure representing a particular players inputs at a world tick.
public struct TickInput {
  public uint WorldTick;

  // The remote world tick the player saw other entities at for this input.
  // (This is equivalent to lastServerWorldTick on the client).
  public uint RemoteViewTick;

  public Player Player;
  public PlayerInputs Inputs;
}

// Processes input network commands from a set of players and presents them in
// a way to the simulation which is easier to interact with.
public class PlayerInputProcessor {
  private SimplePriorityQueue<TickInput> queue = new SimplePriorityQueue<TickInput>();
  private Dictionary<byte, TickInput> latestPlayerInput = new Dictionary<byte, TickInput>();

  // Monitoring.
  private Ice.MovingAverage averageInputQueueSize = new Ice.MovingAverage(10);
  private int staleInputs;

  public void LogQueueStatsForPlayer(Player player, uint worldTick) {
    int count = 0;
    foreach (var entry in queue) {
      if (entry.Player.Id == player.Id && entry.WorldTick >= worldTick) {
        count++;
        worldTick++;
      }
    }
    averageInputQueueSize.Push(count);
    DebugUI.ShowValue("sv avg input queue", averageInputQueueSize.Average());
  }

  public bool TryGetLatestInput(byte playerId, out TickInput ret) {
    return latestPlayerInput.TryGetValue(playerId, out ret);
  }

  public List<TickInput> DequeueInputsForTick(uint worldTick) {
    var ret = new List<TickInput>();
    TickInput entry;
    while (queue.TryDequeue(out entry)) {
      if (entry.WorldTick < worldTick) {
      } else if (entry.WorldTick == worldTick) {
        ret.Add(entry);
      } else {
        // We dequeued a future input, put it back in.
        queue.Enqueue(entry, entry.WorldTick);
        break;
      }
    }
    return ret;
  }

  public void EnqueueInput(NetCommand.PlayerInputCommand command, Player player, uint serverWorldTick) {
    // Monitoring.
    DebugUI.ShowValue("sv stale inputs", staleInputs);

    // Calculate the last tick in the incoming command.
    uint maxTick = command.StartWorldTick + (uint)command.Inputs.Length - 1;

    // Scan for inputs which haven't been handled yet.
    if (maxTick >= serverWorldTick) {
      uint start = serverWorldTick > command.StartWorldTick
          ? serverWorldTick - command.StartWorldTick : 0;
      for (int i = (int)start; i < command.Inputs.Length; ++i) {
        // Apply inputs to the associated player controller and simulate the world.
        var worldTick = command.StartWorldTick + i;
        var tickInput = new TickInput {
          WorldTick = (uint)worldTick,
          RemoteViewTick = (uint)(worldTick - command.ClientWorldTickDeltas[i]),
          Player = player,
          Inputs = command.Inputs[i],
        };
        queue.Enqueue(tickInput, worldTick);

        // Store the latest input in case the simulation needs to repeat missed frames.
        latestPlayerInput[player.Id] = tickInput;
      }
    } else {
      staleInputs++;
    }
  }
}