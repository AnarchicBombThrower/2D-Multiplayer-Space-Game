using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    const int AMOUNT_OF_STARS_PER_GRID_SQUARE = 50;
    const float LOWER_BOUND_OF_STAR_SIZE = 0.2f;
    const float UPPER_BOUND_OF_STAR_SIZE = 1;
    const float LOWER_BOUND_OF_STAR_TRANSPARENCY = 0.6f;
    const float UPPER_BOUND_OF_STAR_TRANSPARENCY = 0.9f;

    private Vector2 sizeOfGridSquare = new Vector2(40, 40);
    [SerializeField]
    private GameObject starPrefab;
    [SerializeField]
    private GameObject topLeftParent;
    [SerializeField]
    private GameObject bottomLeftParent;
    [SerializeField]
    private GameObject topRightParent;
    [SerializeField]
    private GameObject bottomRightParent;

    void Start()
    {
        placeStars(topLeftParent);
        placeStars(bottomLeftParent);
        placeStars(topRightParent);
        placeStars(bottomRightParent);
    }

    // Update is called once per frame
    void Update()
    {
        int xPos = Mathf.RoundToInt(transform.position.x / sizeOfGridSquare.x);
        int yPos = Mathf.RoundToInt(transform.position.y / sizeOfGridSquare.y);
        Vector2Int coord = new Vector2Int(xPos, yPos);

        float offsetRight;
        float offsetLeft;
        float offsetUp;
        float offsetDown;

        if (coord.x % 2 == 0)
        {
            offsetRight = coord.x + 0.5f;
            offsetLeft = coord.x - 0.5f;
        }
        else
        {
            offsetRight = coord.x - 0.5f;
            offsetLeft = coord.x + 0.5f;
        }

        if (coord.y % 2 == 0)
        {
            offsetUp = coord.y + 0.5f;
            offsetDown = coord.y - 0.5f;
        }
        else
        {
            offsetUp = coord.y - 0.5f;
            offsetDown = coord.y + 0.5f;
        }

        topLeftParent.transform.position = new Vector2(offsetLeft * sizeOfGridSquare.x, offsetUp * sizeOfGridSquare.y);
        topRightParent.transform.position = new Vector2(offsetRight * sizeOfGridSquare.x, offsetUp * sizeOfGridSquare.y);
        bottomLeftParent.transform.position = new Vector2(offsetLeft * sizeOfGridSquare.x, offsetDown * sizeOfGridSquare.y);
        bottomRightParent.transform.position = new Vector2(offsetRight * sizeOfGridSquare.x, offsetDown * sizeOfGridSquare.y);
    }

    private void placeStars(GameObject parent)
    {
        for (int i = 0; i < AMOUNT_OF_STARS_PER_GRID_SQUARE; i++)
        {
            Vector2 randomPositionOfStar = new Vector2(
                Random.Range(-sizeOfGridSquare.x * 0.5f, sizeOfGridSquare.x * 0.5f),
                Random.Range(-sizeOfGridSquare.y * 0.5f, sizeOfGridSquare.y * 0.5f));
            float randomRotation = Random.Range(0, 360);
            GameObject newStar = Instantiate(starPrefab, randomPositionOfStar, Quaternion.Euler(0, 0, randomRotation), parent.transform);
            float randomSize = Random.Range(LOWER_BOUND_OF_STAR_SIZE, UPPER_BOUND_OF_STAR_SIZE);
            newStar.transform.localScale = new Vector2(randomSize, randomSize);
            SpriteRenderer starSprite = newStar.GetComponent<SpriteRenderer>();
            float transparency = Random.Range(LOWER_BOUND_OF_STAR_TRANSPARENCY, UPPER_BOUND_OF_STAR_TRANSPARENCY);
            starSprite.color = new Color(1, 1, 1, transparency);
        }
    }
}
