using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;

public class PlayerWonEvent : IGlobalEvent
{
    public Sprite PlayerSprite { get; private set; }
    public PlayerId PlayerId { get; private set; }

    public PlayerWonEvent(Sprite player, PlayerId playerId)
    {
        PlayerSprite = player;
        PlayerId = playerId;
    }
}
