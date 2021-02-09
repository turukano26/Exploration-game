using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC
{
    public int uniqueID;
    public string name;
    public Gender gender;
    public NPC father;
    public NPC mother;
    public List<NPC> children;
    public NPC spouse;
    public Settlement home;
    public Culture culture;
    public int age;
    public Dictionary<NPC, float> opinionOfCharacter;
    public Dictionary<NPC, float> knowledgeOfCharacter;

    public NPC(Settlement home, string name, int id)
    {
        this.home = home;
        this.culture = home.culture;
        this.name = name;
        this.uniqueID = id;
        this.opinionOfCharacter = new Dictionary<NPC, float>();
        this.knowledgeOfCharacter = new Dictionary<NPC, float>();
    }
    public override bool Equals(object obj)
    {
        if(obj == null || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            NPC n = (NPC)obj;
            return (n.uniqueID == this.uniqueID);
        }
    }
    public override int GetHashCode()
    {
        return this.uniqueID;
    }
    public override string ToString()
    {
        string result = "";

        result += "name: " + this.name +"\n";
        result += "id: " + this.uniqueID + "\n";
        result += "friends: \n";
        foreach (KeyValuePair<NPC, float> keyValue in this.opinionOfCharacter)
        {
            result += keyValue.Key.uniqueID + " " + keyValue.Value + "\n";
        }
            
        return result;
    }
}
public enum Gender
{
    male,
    female,
    other
}
