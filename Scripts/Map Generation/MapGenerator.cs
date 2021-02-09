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

    Tile[,] Tiles;
    Continent[] continents;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public TerrainColor[] regions;

    public MapDisplay display;

    public GameObject water;

    public void GenerateMap()
    {
        UpdateWaterSize();
        CreateTiles();
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
    public void CreateTiles()
    {
        Tiles = new Tile[mapWidth, mapHeight];              //the main array for storing the Tiles
        System.Random rnd = new System.Random(seed);

        continents = new Continent[contNum];

        List<int> nextTiles = new List<int>();              //List of tiles to be processed by the while loop (stored as an int that describes its location)

        for (int i = 0; i < contNum;)                       //creates the starting seeds randomly for each continent and adds them to the list
        {
            int tempx = (int)(rnd.NextDouble() * mapWidth);
            int tempy = (int)(rnd.NextDouble() * mapHeight);
            bool isOcean = rnd.NextDouble() >= landRatio;
            Vector2 movement = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());

            if (Tiles[tempx, tempy] == null)
            {
                Continent c = new Continent(i, movement, isOcean);
                continents[i] = c;
                Tiles[tempx, tempy] = new Tile(c, 0);
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

            Continent curCont = Tiles[curX, curY].GetContinent();
            int curContCoreDist = Tiles[curX, curY].getContCoreDist();

            for (int i = 0; i < 4; i++) //loops through each of the 4 neighbour tiles
            {
                int newX = (((xAdd[i] + curX) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                int newY = (((yAdd[i] + curY) % mapHeight) + mapHeight) % mapHeight;

                if (Tiles[newX, newY] == null)    //if the neighbour is currently empty
                {
                    nextTiles.Add(newX + newY * mapWidth);  //add it to the list
                    Tiles[newX, newY] = new Tile(curCont, curContCoreDist + 1); //and give it the same continent
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
                Continent cont = Tiles[i, j].GetContinent();
                if (cont.getIsOcean())
                {
                    Tiles[i, j].height = -10;
                }
                else
                {
                    //float decrease = Mathf.InverseLerp(0, cont.GetSize(), Tiles[i, j].getContCoreDist());
                    //Tiles[i, j].height = (-20 * decrease) + 10;
                    Tiles[i, j].height = 10;
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
                int curID = Tiles[i, j].getID();

                for (int n = 0; n < 4; n++) //loops through each of the 4 neighbour tiles
                {
                    int newX = (((xAdd[n] + i) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                    int newY = (((yAdd[n] + j) % mapHeight) + mapHeight) % mapHeight;

                    if (Tiles[newX, newY].getID() != curID)
                    {
                        //if a land plate is next to an ocean one, smooth out the transition
                        if (Tiles[newX, newY].GetContinent().isOcean != Tiles[i, j].GetContinent().isOcean)
                        {
                            /*for (int p = -shoreSmoothingRadius; p <= shoreSmoothingRadius; p++)
                            {
                                for (int q = -shoreSmoothingRadius + Mathf.Abs(p); q <= shoreSmoothingRadius - Mathf.Abs(p); q++)
                                {
                                    int x = (((newX + p) % mapWidth) + mapWidth) % mapWidth;
                                    int y = (((newY + q) % mapHeight) + mapHeight) % mapHeight;

                                   // float h = Mathf.Lerp(0, Tiles[x, y].height, 0.985f);
                                   float h = Mathf.Lerp(0, Tiles[x, y].height, Mathf.InverseLerp(0,tectonicRadius, (Mathf.Abs(p) + Mathf.Abs(q))));
                                   Tiles[x, y].SetHeight(h);
                                }
                            }*/
                            if(!Tiles[i, j].GetContinent().isOcean) 
                            {
                                if (!nextTiles.Contains(i + j * mapWidth))
                                {
                                    nextTiles.Add(i + j * mapWidth);
                                    distFromOcean[i, j] = 1;
                                    Tiles[i, j].SetHeight(0);
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

                                    Tiles[x, y].addVolcanism(1f / (Mathf.Abs(p) + Mathf.Abs(q) + 1));
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
                    float h = Mathf.Lerp(0, Tiles[newX, newY].height, Mathf.InverseLerp(0, shoreSmoothingRadius, distFromOcean[curX, curY]));
                    Tiles[newX, newY].SetHeight(h);
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
                float v1 = (perlinNeedsTectonic) ? Tiles[i, j].volcanism * tectonicImpact / 20f : 1;
                float v2 = (ridgedNeedsTectonic) ? Tiles[i, j].volcanism * tectonicImpact / 20f : 1;
                Tiles[i, j].IncreaseHeight(perlinMap[i, j] * perlinInfluence * v1);
                Tiles[i, j].IncreaseHeight(ridgedMap[i, j] * ridgedInfluence * v2);
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
                if (Tiles[i, j].height > maxHeight)
                {
                    maxHeight = Tiles[i, j].height;
                }
                if (Tiles[i, j].height < minHeight)
                {
                    minHeight = Tiles[i, j].height;
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
                //result[i, j] = Tiles[i, j].continent.GetSize() / ((float)20 * contNum);
                result[i, j] = Mathf.InverseLerp(minHeight, maxHeight, Tiles[i, j].height)*20;
                //result[i, j] = Tiles[i, j].height;
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
                //colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        return colorMap;
    }
    public void UpdateWaterSize()
    {
        water.gameObject.transform.localScale = new Vector3(-mapWidth/10, 1, mapHeight/10);
    }
}
[System.Serializable]
public struct TerrainColor
{
    public Color color;
    public float height;
}