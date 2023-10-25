using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateWorld : MonoBehaviour
{
    [SerializeField] GameObject _world;

    (int x, int y) _lastPoint = (0, 0);
    (int x, int y) _curPoint;

    Texture2D _texture; //Texture should be set to Read/Write and RGBA32bit in Inspector with no mipmaps

    Material _rendererMaterial;

    Color[] _pixelColors;

    RaycastHit _ray;

    void Start()
    {
        Application.targetFrameRate = 150;

        // get the material of the world
        _rendererMaterial = _world.GetComponent<Renderer>().material;

        // duplicate original texture
        _texture = Instantiate(_rendererMaterial.mainTexture) as Texture2D;

        // assign to material
        _rendererMaterial.mainTexture = _texture;

        _pixelColors = new Color[_texture.width * _texture.height];

        Debug.Log($"texture w,h: {_texture.width},{_texture.height}");

        // store yellow cells using perlin noise to create some patches of "minerals"
        for (int y = 0; y < _texture.height; y++)
        {
            for (int x = 0; x < _texture.width; x++)
            {
                float perlinNoise = Mathf.PerlinNoise(x / 20.0f, y / 20.0f);

                int coord = getColorsIndex(x, y);

                _pixelColors[coord] = perlinNoise < 0.4 ? Color.yellow : Color.white;
            }
        }

        // sets pixel data for the texture in CPU memory
        _texture.SetPixels(_pixelColors);

        // upload the changed pixels to the GPU
        _texture.Apply();
    }

    int getColorsIndex(int x, int y)
    {
        int index = y * _texture.width + x;
        return Math.Clamp(index, 0, _pixelColors.Length - 1);
    }

    void Update()
    {
        bool dirty = false;

        //record start of mouse drawing (or erasing) to get the first position the mouse touches down
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (raycastCurMouse())
            {
                _lastPoint = ((int)(_ray.textureCoord.x * _texture.width),
                              (int)(_ray.textureCoord.y * _texture.height));
            }
        }

        //draw a line between the last known location of the mouse and the current location
        if (Input.GetMouseButton(0))
        {
            if (raycastCurMouse())
            {
                dirty = true;

                _curPoint = ((int)(_ray.textureCoord.x * _texture.width),
                             (int)(_ray.textureCoord.y * _texture.height));

                DrawPixelLine(_curPoint.x,
                              _curPoint.y,
                              _lastPoint.x,
                              _lastPoint.y,
                              Color.black);

                _lastPoint = new(_curPoint.x, _curPoint.y);
            }
        }

        if (Input.GetMouseButton(1))
        {
            if (raycastCurMouse())
            {
                dirty = true;

                _pixelColors[getColorsIndex((int)(_ray.textureCoord.x * _texture.width), (int)(_ray.textureCoord.y * _texture.height))] = Color.white;
            }
        }

        if (dirty)
        {
            dirty = SimulateWorld();

            if (dirty)
            {
                _texture.SetPixels(_pixelColors);
                _texture.Apply();
            }
        }
    }

    bool raycastCurMouse()
    {
        bool raycast = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out _ray);
        return raycast;
    }

    int CountNeighbourColor(int x, int y, Color col)
    {
        int count = 0;
        //loop through all 8 neighbouring cells and if their colour
        //is the one passed through then count it
        for (int ny = -1; ny < 2; ny++)
        {
            for (int nx = 0 - 1; nx < 2; nx++)
            {
                if (ny == 0 && nx == 0) continue; //ignore cell you are looking at neighbours
                if (_pixelColors[getColorsIndex(x + nx, y + ny)] == col)
                    count++;
            }
        }
        return count;
    }

    bool SimulateWorld()
    {
        bool dirty = false;

        for (int y = 0; y < _texture.height; y++)
        {
            for (int x = 0; x < _texture.width; x++)
            {
                int blackNeibors = CountNeighbourColor(x, y, Color.black);
                Color curPxColor = _pixelColors[getColorsIndex(x, y)];

                if (blackNeibors > 4)
                {
                    //if a cell has more than 4 black neighbours make it blue
                    //Commercial Property
                    dirty = true;
                    _pixelColors[getColorsIndex(x, y)] = Color.blue;
                }
                else if (blackNeibors > 0)
                {
                    if (curPxColor == Color.white)
                    {
                        //if a cell has a black neighbour and is not black itself
                        //set to green
                        //Residential Property
                        dirty = true;
                        _pixelColors[getColorsIndex(x, y)] = Color.green;
                    }
                    else if (curPxColor == Color.yellow)
                    {
                        //if near a black cell but the cell is already yellow
                        //Mining Property
                        dirty = true;
                        _pixelColors[getColorsIndex(x, y)] = Color.magenta;

                    }
                }
                else if (blackNeibors == 0)
                {
                    //if a cell is blue, green or magenta and has no black next to it then it should die = turn white)
                    //if road is taken away the cell should die/deallocate property
                    if (curPxColor == Color.green ||
                        curPxColor == Color.blue ||
                        curPxColor == Color.magenta)
                    {
                        dirty = true;
                        _pixelColors[getColorsIndex(x, y)] = Color.white;
                    }
                }
            }
        }

        return dirty;
    }




    //Draw a pixel by pixel line between two points
    //For more information on the algorithm see: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
    //DO NOT MODIFY OR OPTMISE
    void DrawPixelLine(int x, int y, int x2, int y2, Color color)
    {
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            _pixelColors[getColorsIndex(x, y)] = color;
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
        //texture.Apply();
    }
}
