using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelNumber;
    public int conveyorCapacity = 10;
    public int boxCountPerSide = 1;    // 1 = level1, 2 = level2, etc
    public int colorsCount = 2;        // how many colors in this level
    public int flasksPerBox = 4;       // capacity of each box
    public int openBoxesPerSide = 1;
}

[System.Serializable]
public class BoxConfig
{
    public FlaskColor boxColor;
    public int flaskCount = 4;
    public bool isHidden = false;
    public bool startOpen = false;
    public List<FlaskColor> flasksInside;
}

public enum FlaskColor
{
    Blue,    // 0 → #5DA7CE
    Pink,    // 1 → #DB75BF
    Yellow,  // 2 → #E7E169
    Red,     // 3 → #E73C31
    Purple   // 4 → #974DC7
}