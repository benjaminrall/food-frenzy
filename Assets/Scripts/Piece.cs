using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PieceType
{
    Default,
    Horizontal,
    Vertical,
    Rainbow
}

public class Piece : MonoBehaviour
{
    public Sprite rainbowSprite;
    
    public int X { get; set; }
    public int Y { get; set; }
    public int Colour { get; private set; }
    public PieceType Type { get; private set; }

    public void Initialise(int x, int y, int colour, PieceType type = PieceType.Default)
    {
        X = x;
        Y = y;
        Colour = colour;
        Type = type;

        if (type == PieceType.Rainbow)
        {
            Colour = -1;
            GetComponent<SpriteRenderer>().sprite = rainbowSprite;
        }
    }
}
