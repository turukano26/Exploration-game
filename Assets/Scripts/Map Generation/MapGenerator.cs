using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //base settings
    public bool autoUpdateMap;
    public int mapWidth;
    public int mapHeight;
    public int seed;
    public int contNum;
    public float landRatio;

    //perlin noise settings
    public float perlinScale;
    public int perlinOctaves;
    public float perlinPersistance;
    public float perlinLacunarity;
    public float perlinInfluence;

    //ridged noise settings
    public float ridgedScale;
    public int ridgedOctaves;
    public float ridgedPersistance;

    [Range(1f,3f)]
    public float ridgedLacunarity;
    public float ridgedInfluence;

    //plate edge settings
    public bool perlinNeedsTectonic;
    public bool ridgedNeedsTectonic;
    public float tectonicImpact;
    public int tectonicRadius;
    public int shoreSmoothingRadius;
    public int test;

    //arrays used for visiting the 4 directly adjacent tiles
    readonly int[] xAdd = new int[] { 1, 0, -1, 0 };             
    readonly int[] yAdd = new int[] { 0, 1, 0, -1 };

    float maxHeight = 0;
    float minHeight = 0;

    //arrays to store the info per tile
    public float[,] heightArray;
    public int[,] contCoreDistArray;
    public int[,] continentIDsArray;
    public float[,] volcanismArray;


    Continent[] continents;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public TerrainColor[] regions;

    public MapDisplay display;

    public GameObject water;

    public void GenerateMap()
    {
        Initialize();
        MapContinents();
        CalculateBaseHeights();
        FixContinentEdges();
        AddNoise();
        RecalculateMinMax();

        //MapDisplay display = FindObjectOfType<MapDisplay>();

        float[,] heightMap = CreateHeightMap();
        Color[] colorMap = GenerateColorMap(heightMap);

        //Render3D(HeightMap);
        display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap), TextureGenerator.TextureFromColourMap(colorMap, mapWidth, mapHeight));
    }

    public void Initialize()
    {
        water.gameObject.transform.localScale = new Vector3(-mapWidth / 10, 1, mapHeight / 10); //sets the water plane to the map size

        heightArray = new float[mapWidth, mapHeight];                //the main arrays for storing the tile data
        volcanismArray = new float[mapWidth, mapHeight];
        contCoreDistArray = new int[mapWidth, mapHeight];
        continentIDsArray = new int[mapWidth, mapHeight];

        for(int i = 0; i < mapWidth; i++)                           //sets the initial values for contIDs to int.maxvalue
        {
            for (int j = 0; j < mapHeight; j++)
            {
                continentIDsArray[i, j] = int.MaxValue;
            }
        }
        continents = new Continent[contNum];
    }
    public void MapContinents()
    {
        System.Random rnd = new System.Random(seed);

        List<int> nextTiles = new List<int>();              //List of tiles to be processed by the while loop (stored as an int that describes its location)

        for (int i = 0; i < contNum;)                       //creates the starting seeds randomly for each continent and adds them to the list
        {
            int tempx = (int)(rnd.NextDouble() * mapWidth);
            int tempy = (int)(rnd.NextDouble() * mapHeight);
            bool isOcean = rnd.NextDouble() >= landRatio;
            Vector2 movement = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());

            if (continentIDsArray[tempx, tempy] == int.MaxValue)
            {
                Continent c = new Continent(i, movement, isOcean);
                continents[i] = c;
                continentIDsArray[tempx, tempy] = i;
                contCoreDistArray[tempx, tempy] = 0;
                nextTiles.Add(tempx + tempy * mapWidth);
                i++;
            }
        }

        while (nextTiles.Count > 0)
        {
            int temp = (int)(rnd.NextDouble() * nextTiles.Count);        //picks a random tile from the list
            int curTile = nextTiles[temp];
            nextTiles.RemoveAt(temp);                                   //and removes it from the list

            int curX = curTile % mapWidth;                              //gets the x and y of the tile
            int curY = curTile / mapWidth;

            int curContID = continentIDsArray[curX, curY];
            int curContCoreDist = contCoreDistArray[curX, curY];

            for (int i = 0; i < 4; i++) //loops through each of the 4 neighbour tiles
            {
                int newX = (((xAdd[i] + curX) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                int newY = (((yAdd[i] + curY) % mapHeight) + mapHeight) % mapHeight;

                if (continentIDsArray[newX, newY] == int.MaxValue)    //if the neighbour is currently empty
                {
                    nextTiles.Add(newX + newY * mapWidth);  //add it to the list
                    continentIDsArray[newX, newY] = curContID;
                    contCoreDistArray[newX, newY] = curContCoreDist + 1;
                }
            }
        }
    }

    //gives continents and oceans their base heights
    public void CalculateBaseHeights()
    {
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                Continent cont = continents[continentIDsArray[i, j]];
                if (cont.getIsOcean())
                {
                    heightArray[i, j] = -10;
                }
                else
                {
                    heightArray[i, j] = 10;
                }

            }
        }
    }

    //Adds Volcanism to continent edges and also fixes the jump from ocean to land plates
    public void FixContinentEdges()
    {
        List<int> nextTiles = new List<int>();
        int[,] distFromOcean = new int[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                int curID = continentIDsArray[i, j];

                for (int n = 0; n < 4; n++) //loops through each of the 4 neighbour tiles
                {
                    int newX = (((xAdd[n] + i) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                    int newY = (((yAdd[n] + j) % mapHeight) + mapHeight) % mapHeight;

                    if (continentIDsArray[newX, newY] != curID)
                    {
                        //if a land plate is next to an ocean one, smooth out the transition
                        if (continents[continentIDsArray[newX, newY]].isOcean != continents[continentIDsArray[i, j]].isOcean)
                        {
                            if(!continents[continentIDsArray[i, j]].isOcean) 
                            {
                                if (!nextTiles.Contains(i + j * mapWidth))
                                {
                                    nextTiles.Add(i + j * mapWidth);
                                    distFromOcean[i, j] = 1;
                                    heightArray[i, j] = 0;
                                }
                            }
                        }
                        else
                        {
                            //adds volcanism
                            for (int p = -tectonicRadius; p <= tectonicRadius; p++)
                            {
                                for (int q = -tectonicRadius + Mathf.Abs(p); q <= tectonicRadius - Mathf.Abs(p); q++)
                                {
                                    int x = (((newX + p) % mapWidth) + mapWidth) % mapWidth;
                                    int y = (((newY + q) % mapHeight) + mapHeight) % mapHeight;

                                    volcanismArray[x, y] += (1f / (Mathf.Abs(p) + Mathf.Abs(q) + 1));
                                }
                            }
                        }
                    }
                }
            }
        }
        while (nextTiles.Count > 0)
        {
            int curTile = nextTiles[0];
            nextTiles.RemoveAt(0);                                   //and removes it from the list

            int curX = curTile % mapWidth;                              //gets the x and y of the tile
            int curY = curTile / mapWidth;

            for (int i = 0; i < 4; i++) //loops through each of the 4 neighbour tiles
            {
                int newX = (((xAdd[i] + curX) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                int newY = (((yAdd[i] + curY) % mapHeight) + mapHeight) % mapHeight;

                if (distFromOcean[newX,newY] == 0 && distFromOcean[curX, curY] < shoreSmoothingRadius)  
                {
                    nextTiles.Add(newX + newY * mapWidth);  //add it to the list
                    distFromOcean[newX, newY] = distFromOcean[curX, curY] + 1;
                    float h = Mathf.Lerp(0, heightArray[newX, newY], Mathf.InverseLerp(0, shoreSmoothingRadius, distFromOcean[curX, curY]));
                    heightArray[newX, newY] = h;
                }
            }
        }
    }

    public void AddNoise()
    {
        float[,] perlinMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, perlinScale, perlinOctaves, perlinPersistance, perlinLacunarity, Vector2.zero, false);
        float[,] ridgedMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, ridgedScale, ridgedOctaves, ridgedPersistance, ridgedLacunarity, Vector2.zero, true);

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                float v1 = (perlinNeedsTectonic) ? volcanismArray[i, j] * tectonicImpact / 20f : 1;
                float v2 = (ridgedNeedsTectonic) ? volcanismArray[i, j] * tectonicImpact / 20f : 1;
                heightArray[i, j] += (perlinMap[i, j] * perlinInfluence * v1);
                heightArray[i, j] += (ridgedMap[i, j] * ridgedInfluence * v2);
            }
        }
    }

    public void RecalculateMinMax()
    {
        maxHeight = 0;
        minHeight = 0;
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                if (heightArray[i, j] > maxHeight)
                {
                    maxHeight = heightArray[i, j];
                }
                if (heightArray[i, j] < minHeight)
                {
                    minHeight = heightArray[i, j];
                }
            }
        }
    }
    public float[,] CreateHeightMap()
    { 
        float[,] result = new float[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                result[i, j] = Mathf.InverseLerp(minHeight, maxHeight, heightArray[i, j])*20;
                //result[i, j] = heightArray[i, j];
            }
        }
        return result;
    }
    public Color[] GenerateColorMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int k = 0; k < regions.Length; k++)
                {
                    if (noiseMap[x, y] <= regions[k].height)
                    {
                        colorMap[y * width + x] = regions[k].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }
}
[System.Serializable]
public struct TerrainColor
{
    public Color color;
    public float height;
}