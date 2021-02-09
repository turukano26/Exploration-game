using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Continent
{
    public int ID;
    public int contSize;
    public Vector2 movement;
    public bool isOcean;
    

    public Continent(int id, Vector2 m, bool b)
    {
        this.ID = id;
        this.movement = m;
        this.isOcean = b;
    }
    public void UpdateSize(int s)
    {
        contSize = s;
    }
    public int GetSize()
    {
        return contSize;
    }
    public bool getIsOcean()
    {
        return this.isOcean;
    }
}
