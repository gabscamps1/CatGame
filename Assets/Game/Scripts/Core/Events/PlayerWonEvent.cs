using CatGame.Core.Interfaces;
using UnityEngine;

public class PlayerWonEvent : IGlobalEvent
{
    public Sprite PlayerSprite { get; private set; }
    public string PlayerId { get; private set; }

    public PlayerWonEvent(Sprite player, string playerId)
    {
        PlayerSprite = player;
        PlayerId = playerId;
    }
}
