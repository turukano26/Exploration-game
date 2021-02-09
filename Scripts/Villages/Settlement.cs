using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
    public string Sname;
    public Vector2 location;
    public Culture culture;
    public int size;
    public int seed;
    public int age = 50;
    public List<NPC> residents;

    System.Random rnd;

    readonly static int[] friendshipIncreases = {1,1,2,3,4,9};

    public void Start()
    {
        rnd = new System.Random(seed);
        residents = new List<NPC>();

        Populate();
        SimulateYear(age);


        foreach(NPC n in residents)
        {
            Debug.Log(n.ToString());
        }
    }
    public Settlement(Vector2 location, int seed, int size, Culture culture)
    {
        this.location = location;
        this.seed = seed;
        this.size = size;
        this.culture = culture;
        rnd = new System.Random(seed);

        Populate();
        SimulateYear(age);
    }

    public void Populate()
    {
        for (int i = 0; i < size; i++)
        {
            string n = this.culture.possibleNames[(int) (this.culture.possibleNames.Count * rnd.NextDouble())];
            NPC c = new NPC(this, n,rnd.Next(int.MinValue,int.MaxValue));
            this.residents.Add(c);
        }
    }

    public void SimulateYear(int years)
    {
        for (int i = 0; i < years; i++)
        {
            Friendships();
            //Marriages();
        }
    }

    public void Friendships()
    {
        foreach (NPC n in residents)
        {
            //add a new friend
            NPC r = residents[(int)(residents.Count * rnd.NextDouble())];
            if (!n.Equals(r) && !n.opinionOfCharacter.ContainsKey(r))
            {
                n.opinionOfCharacter.Add(r, 5);
                r.opinionOfCharacter.Add(n, 5);
                n.knowledgeOfCharacter.Add(r, 5);
                r.knowledgeOfCharacter.Add(n, 5);
            }
            //increase old friendships
            List<NPC> oldFriends = new List<NPC>();
            foreach (KeyValuePair<NPC, float> keyValue in n.opinionOfCharacter)
            {
                oldFriends.Add(keyValue.Key);
            }
            foreach(NPC p in oldFriends)
            {
                n.opinionOfCharacter[p] += friendshipIncreases[(int)(rnd.NextDouble() * friendshipIncreases.Length)];
                n.knowledgeOfCharacter[p] += 5;
            }
            //TODO: make the friendship increase depend on how much the other likes you
        }
    }

    public void Marriages()
    {
        //makes two lists of all unmarried NPCs who are old enough to be married
        List<NPC> bachelorettes = new List<NPC>();
        List<NPC> bachelors = new List<NPC>();
        foreach (NPC n in residents)
        {
            if(n.age >= this.culture.ageOfMarrige && n.spouse == null)
            {
                if (n.gender == Gender.female)
                {
                    bachelorettes.Add(n);
                }
                else if(n.gender == Gender.male)
                {
                    bachelors.Add(n);
                }
            }
        }

        while(bachelors.Count > 0)
        {

        }
    }
}
