using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CutController : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Renderer foodRenderer;

    [Header("ステージ")]
    [SerializeField] private GameObject[] foods;
    private int currentFoodIndex = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text clearText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject nextButton;

    [Header("NEXTに進める評価")]
    [SerializeField] private bool perfectCanNext = true;
    [SerializeField] private bool greatCanNext = true;
    [SerializeField] private bool goodCanNext = true;
    [SerializeField] private bool tryAgainCanNext = false;

    [Header("Sprite面積判定")]
    [SerializeField] private float alphaThreshold = 0.1f;

    [Header("切断演出")]
    [SerializeField] private float splitDistance = 0.25f;
    [SerializeField] private float splitDuration = 0.15f;
    [SerializeField] private float cutLineWidth = 0.18f;
    [SerializeField] private float normalLineWidth = 0.08f;

    private Camera mainCamera;

    private Vector2 startPoint;
    private Vector2 endPoint;

    private bool isDragging = false;
    private bool hasCut = false;

    private GameObject splitObjectA;
    private GameObject splitObjectB;


    // ==================================================
    // 初期化
    // ==================================================

    void Start()
    {
        mainCamera = Camera.main;

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        lineRenderer.startWidth = normalLineWidth;
        lineRenderer.endWidth = normalLineWidth;

        resultText.gameObject.SetActive(false);
        clearText.gameObject.SetActive(false);

        retryButton.SetActive(false);
        nextButton.SetActive(false);

        currentFoodIndex = 0;

        if (foods.Length > 0)
        {
            for (int i = 0; i < foods.Length; i++)
            {
                foods[i].SetActive(i == 0);
            }

            SetCurrentFood();
        }
    }


    // ==================================================
    // 入力
    // ==================================================

    void Update()
    {
        if (hasCut)
            return;

        // スマホ
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame ||
                touch.press.isPressed ||
                touch.press.wasReleasedThisFrame)
            {
                HandleTouch();
                return;
            }
        }

        // PC
        if (Mouse.current != null)
        {
            HandleMouse();
        }
    }


    void HandleMouse()
    {
        Vector2 position =
            Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCut(position);
        }

        if (Mouse.current.leftButton.isPressed &&
            isDragging)
        {
            UpdateCut(position);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame &&
            isDragging)
        {
            EndCut(position);
        }
    }


    void HandleTouch()
    {
        var touch =
            Touchscreen.current.primaryTouch;

        Vector2 position =
            touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            StartCut(position);
        }

        if (touch.press.isPressed &&
            isDragging)
        {
            UpdateCut(position);
        }

        if (touch.press.wasReleasedThisFrame &&
            isDragging)
        {
            EndCut(position);
        }
    }


    // ==================================================
    // 切断
    // ==================================================

    void StartCut(Vector2 screenPosition)
    {
        isDragging = true;

        startPoint =
            ScreenToWorld(screenPosition);

        endPoint = startPoint;

        lineRenderer.enabled = true;

        SetLinePositions();
    }


    void UpdateCut(Vector2 screenPosition)
    {
        endPoint =
            ScreenToWorld(screenPosition);

        SetLinePositions();
    }


    void EndCut(Vector2 screenPosition)
    {
        endPoint =
            ScreenToWorld(screenPosition);

        SetLinePositions();

        isDragging = false;

        if (!LineCrossesFood(startPoint, endPoint))
        {
            Debug.Log(
                "Foodを端から端まで切ってください"
            );

            lineRenderer.enabled = false;
            return;
        }

        SpriteRenderer spriteRenderer =
            foodRenderer as SpriteRenderer;

        if (spriteRenderer == null)
        {
            Debug.LogWarning(
                "現在のFoodにSpriteRendererがありません"
            );

            lineRenderer.enabled = false;
            return;
        }

        hasCut = true;

        CalculateSpriteAreaRatio();
        SplitSprite(spriteRenderer);

        StartCoroutine(CutLineEffect());
    }


    void SetLinePositions()
    {
        lineRenderer.SetPosition(
            0,
            new Vector3(
                startPoint.x,
                startPoint.y,
                -0.1f
            )
        );

        lineRenderer.SetPosition(
            1,
            new Vector3(
                endPoint.x,
                endPoint.y,
                -0.1f
            )
        );
    }


    // ==================================================
    // Spriteの実面積
    // ==================================================

    void CalculateSpriteAreaRatio()
    {
        SpriteRenderer spriteRenderer =
            foodRenderer as SpriteRenderer;

        if (spriteRenderer == null)
            return;

        Sprite sprite =
            spriteRenderer.sprite;

        Texture2D texture =
            sprite.texture;

        Rect rect =
            sprite.textureRect;

        int sideACount = 0;
        int sideBCount = 0;

        for (int y = 0; y < (int)rect.height; y++)
        {
            for (int x = 0; x < (int)rect.width; x++)
            {
                Color pixel =
                    texture.GetPixel(
                        (int)rect.x + x,
                        (int)rect.y + y
                    );

                // 透明部分は面積に含めない
                if (pixel.a <= alphaThreshold)
                    continue;

                float normalizedX =
                    (x + 0.5f) / rect.width;

                float normalizedY =
                    (y + 0.5f) / rect.height;

                Vector2 localPosition =
                    new Vector2(
                        sprite.bounds.min.x +
                        normalizedX *
                        sprite.bounds.size.x,

                        sprite.bounds.min.y +
                        normalizedY *
                        sprite.bounds.size.y
                    );

                Vector3 worldPosition =
                    spriteRenderer.transform.TransformPoint(
                        localPosition
                    );

                float side =
                    SideOfLine(
                        startPoint,
                        endPoint,
                        new Vector2(
                            worldPosition.x,
                            worldPosition.y
                        )
                    );

                if (side >= 0f)
                    sideACount++;
                else
                    sideBCount++;
            }
        }

        int total =
            sideACount + sideBCount;

        if (total <= 0)
            return;

        float percentA =
            (float)sideACount /
            total *
            100f;

        float percentB =
            (float)sideBCount /
            total *
            100f;

        ShowResult(
            percentA,
            percentB
        );
    }


    // ==================================================
    // Spriteを2つに分割
    // ==================================================

    void SplitSprite(SpriteRenderer originalRenderer)
    {
        Sprite sprite =
            originalRenderer.sprite;

        Texture2D sourceTexture =
            sprite.texture;

        Rect rect =
            sprite.textureRect;

        int width =
            (int)rect.width;

        int height =
            (int)rect.height;


        Texture2D textureA =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

        Texture2D textureB =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );


        textureA.filterMode =
            sourceTexture.filterMode;

        textureB.filterMode =
            sourceTexture.filterMode;


        Color clear =
            new Color(0f, 0f, 0f, 0f);


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel =
                    sourceTexture.GetPixel(
                        (int)rect.x + x,
                        (int)rect.y + y
                    );


                float normalizedX =
                    (x + 0.5f) /
                    width;

                float normalizedY =
                    (y + 0.5f) /
                    height;


                Vector2 localPosition =
                    new Vector2(
                        sprite.bounds.min.x +
                        normalizedX *
                        sprite.bounds.size.x,

                        sprite.bounds.min.y +
                        normalizedY *
                        sprite.bounds.size.y
                    );


                Vector3 worldPosition =
                    originalRenderer.transform.TransformPoint(
                        localPosition
                    );


                float side =
                    SideOfLine(
                        startPoint,
                        endPoint,
                        new Vector2(
                            worldPosition.x,
                            worldPosition.y
                        )
                    );


                if (side >= 0f)
                {
                    textureA.SetPixel(
                        x,
                        y,
                        pixel
                    );

                    textureB.SetPixel(
                        x,
                        y,
                        clear
                    );
                }
                else
                {
                    textureA.SetPixel(
                        x,
                        y,
                        clear
                    );

                    textureB.SetPixel(
                        x,
                        y,
                        pixel
                    );
                }
            }
        }


        textureA.Apply();
        textureB.Apply();


        float pixelsPerUnit =
            sprite.pixelsPerUnit;


        Vector2 pivot =
            new Vector2(
                sprite.pivot.x /
                rect.width,

                sprite.pivot.y /
                rect.height
            );


        Sprite spriteA =
            Sprite.Create(
                textureA,
                new Rect(
                    0,
                    0,
                    width,
                    height
                ),
                pivot,
                pixelsPerUnit
            );


        Sprite spriteB =
            Sprite.Create(
                textureB,
                new Rect(
                    0,
                    0,
                    width,
                    height
                ),
                pivot,
                pixelsPerUnit
            );


        splitObjectA =
            CreateSplitSpriteObject(
                "Food_Split_A",
                spriteA,
                originalRenderer
            );


        splitObjectB =
            CreateSplitSpriteObject(
                "Food_Split_B",
                spriteB,
                originalRenderer
            );


        // 元画像を非表示
        originalRenderer.enabled = false;

        // 切断線の方向
        Vector2 cutDirection =
            (endPoint - startPoint).normalized;

        // 切断線に対して90度の方向
        Vector2 separationDirection =
            new Vector2(
                -cutDirection.y,
                cutDirection.x
            ).normalized;

        // AはSideOfLine >= 0 のピクセルなので
        // 常に法線方向へ移動
        StartCoroutine(
            MoveSplitPieces(
                separationDirection
            )
        );

    }


    // ==================================================
    // 分割Spriteオブジェクト生成
    // ==================================================

    GameObject CreateSplitSpriteObject(
        string objectName,
        Sprite sprite,
        SpriteRenderer originalRenderer
    )
    {
        GameObject obj =
            new GameObject(objectName);


        obj.transform.position =
            originalRenderer.transform.position;


        obj.transform.rotation =
            originalRenderer.transform.rotation;


        obj.transform.localScale =
            originalRenderer.transform.lossyScale;


        SpriteRenderer renderer =
            obj.AddComponent<SpriteRenderer>();


        renderer.sprite =
            sprite;


        renderer.color =
            originalRenderer.color;


        renderer.flipX =
            originalRenderer.flipX;


        renderer.flipY =
            originalRenderer.flipY;


        renderer.sortingLayerID =
            originalRenderer.sortingLayerID;


        renderer.sortingOrder =
            originalRenderer.sortingOrder;


        return obj;
    }


    // ==================================================
    // 分割した2つを離す
    // ==================================================

    IEnumerator MoveSplitPieces(
        Vector2 direction
    )
    {
        if (splitObjectA == null ||
            splitObjectB == null)
        {
            yield break;
        }


        Vector3 startA =
            splitObjectA.transform.position;


        Vector3 startB =
            splitObjectB.transform.position;


        Vector3 movement =
            new Vector3(
                direction.x,
                direction.y,
                0f
            ) *
            splitDistance;


        Vector3 targetA =
            startA + movement;


        Vector3 targetB =
            startB - movement;


        float time = 0f;


        while (time < splitDuration)
        {
            if (splitObjectA == null ||
                splitObjectB == null)
            {
                yield break;
            }


            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time /
                    splitDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            splitObjectA.transform.position =
                Vector3.Lerp(
                    startA,
                    targetA,
                    t
                );


            splitObjectB.transform.position =
                Vector3.Lerp(
                    startB,
                    targetB,
                    t
                );


            yield return null;
        }


        splitObjectA.transform.position =
            targetA;


        splitObjectB.transform.position =
            targetB;
    }

    // ==================================================
    // 結果表示
    // ==================================================

    void ShowResult(
        float percentA,
        float percentB
    )
    {
        string result =
            percentA.ToString("F1") +
            "% : " +
            percentB.ToString("F1") +
            "%";


        float difference =
            Mathf.Abs(
                percentA - 50f
            );


        string rank;


        if (difference <= 1f)
            rank = "PERFECT";

        else if (difference <= 3f)
            rank = "GREAT";

        else if (difference <= 5f)
            rank = "GOOD";

        else
            rank = "TRY AGAIN";


        bool canNext = false;


        if (rank == "PERFECT")
            canNext = perfectCanNext;

        else if (rank == "GREAT")
            canNext = greatCanNext;

        else if (rank == "GOOD")
            canNext = goodCanNext;

        else if (rank == "TRY AGAIN")
            canNext = tryAgainCanNext;


        resultText.text =
            result +
            "\n" +
            rank;


        resultText.gameObject.SetActive(true);


        if (canNext)
        {
            retryButton.SetActive(false);


            // 最後のFoodなら即CLEAR
            if (currentFoodIndex ==
                foods.Length - 1)
            {
                nextButton.SetActive(false);

                clearText.gameObject.SetActive(
                    true
                );
            }
            else
            {
                nextButton.SetActive(true);
            }
        }
        else
        {
            retryButton.SetActive(true);
            nextButton.SetActive(false);
        }


        Debug.Log(
            "結果：" +
            result +
            " / " +
            rank
        );
    }


    // ==================================================
    // NEXT
    // ==================================================

    public void Next()
    {
        DestroySplitObjects();


        SpriteRenderer currentSprite =
            foodRenderer as SpriteRenderer;


        if (currentSprite != null)
        {
            currentSprite.enabled = true;
        }


        foods[currentFoodIndex]
            .SetActive(false);


        currentFoodIndex++;


        if (currentFoodIndex >=
            foods.Length)
        {
            resultText.gameObject.SetActive(
                false
            );

            retryButton.SetActive(false);
            nextButton.SetActive(false);

            clearText.gameObject.SetActive(
                true
            );

            return;
        }


        foods[currentFoodIndex]
            .SetActive(true);


        SetCurrentFood();

        ResetUI();


        hasCut = false;
        isDragging = false;
    }


    // ==================================================
    // RETRY
    // ==================================================

    public void Retry()
    {
        StopAllCoroutines();


        DestroySplitObjects();


        SpriteRenderer currentSprite =
            foodRenderer as SpriteRenderer;


        if (currentSprite != null)
        {
            currentSprite.enabled = true;
        }


        lineRenderer.enabled = false;

        lineRenderer.startWidth =
            normalLineWidth;

        lineRenderer.endWidth =
            normalLineWidth;


        ResetUI();


        isDragging = false;
        hasCut = false;
    }


    // ==================================================
    // 分割オブジェクト削除
    // ==================================================

    void DestroySplitObjects()
    {
        if (splitObjectA != null)
        {
            SpriteRenderer renderer =
                splitObjectA.GetComponent<SpriteRenderer>();

            if (renderer != null &&
                renderer.sprite != null)
            {
                Texture2D texture =
                    renderer.sprite.texture;

                Destroy(renderer.sprite);
                Destroy(texture);
            }

            Destroy(splitObjectA);

            splitObjectA = null;
        }


        if (splitObjectB != null)
        {
            SpriteRenderer renderer =
                splitObjectB.GetComponent<SpriteRenderer>();

            if (renderer != null &&
                renderer.sprite != null)
            {
                Texture2D texture =
                    renderer.sprite.texture;

                Destroy(renderer.sprite);
                Destroy(texture);
            }

            Destroy(splitObjectB);

            splitObjectB = null;
        }
    }


    // ==================================================
    // 現在のFood
    // ==================================================

    void SetCurrentFood()
    {
        foodRenderer =
            foods[currentFoodIndex]
            .GetComponent<Renderer>();


        if (foodRenderer == null)
        {
            Debug.LogError(
                foods[currentFoodIndex].name +
                " にRendererがありません"
            );
        }
    }


    // ==================================================
    // UIリセット
    // ==================================================

    void ResetUI()
    {
        resultText.text = "";

        resultText.gameObject.SetActive(false);

        clearText.gameObject.SetActive(false);

        retryButton.SetActive(false);
        nextButton.SetActive(false);
    }


    // ==================================================
    // Foodを端から端まで切ったか
    // ==================================================

    bool LineCrossesFood(
        Vector2 start,
        Vector2 end
    )
    {
        Bounds bounds =
            foodRenderer.bounds;


        Vector2 bottomLeft =
            new Vector2(
                bounds.min.x,
                bounds.min.y
            );


        Vector2 bottomRight =
            new Vector2(
                bounds.max.x,
                bounds.min.y
            );


        Vector2 topLeft =
            new Vector2(
                bounds.min.x,
                bounds.max.y
            );


        Vector2 topRight =
            new Vector2(
                bounds.max.x,
                bounds.max.y
            );


        int hitCount = 0;


        if (LinesIntersect(
            start,
            end,
            bottomLeft,
            bottomRight))
        {
            hitCount++;
        }


        if (LinesIntersect(
            start,
            end,
            topLeft,
            topRight))
        {
            hitCount++;
        }


        if (LinesIntersect(
            start,
            end,
            bottomLeft,
            topLeft))
        {
            hitCount++;
        }


        if (LinesIntersect(
            start,
            end,
            bottomRight,
            topRight))
        {
            hitCount++;
        }


        return hitCount >= 2;
    }


    bool LinesIntersect(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d
    )
    {
        float denominator =
            (b.x - a.x) *
            (d.y - c.y)
            -
            (b.y - a.y) *
            (d.x - c.x);


        if (Mathf.Approximately(
            denominator,
            0f))
        {
            return false;
        }


        float t =
            ((c.x - a.x) *
             (d.y - c.y)
             -
             (c.y - a.y) *
             (d.x - c.x))
            /
            denominator;


        float u =
            ((c.x - a.x) *
             (b.y - a.y)
             -
             (c.y - a.y) *
             (b.x - a.x))
            /
            denominator;


        return
            t >= 0f &&
            t <= 1f &&
            u >= 0f &&
            u <= 1f;
    }


    // ==================================================
    // 線のどちら側か
    // ==================================================

    float SideOfLine(
        Vector2 a,
        Vector2 b,
        Vector2 point
    )
    {
        return
            (b.x - a.x) *
            (point.y - a.y)
            -
            (b.y - a.y) *
            (point.x - a.x);
    }


    // ==================================================
    // Screen → World
    // ==================================================

    Vector2 ScreenToWorld(
        Vector2 screenPosition
    )
    {
        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    -mainCamera.transform.position.z
                )
            );


        return new Vector2(
            worldPosition.x,
            worldPosition.y
        );
    }


    // ==================================================
    // 切断線演出
    // ==================================================

    IEnumerator CutLineEffect()
    {
        lineRenderer.startWidth =
            cutLineWidth;

        lineRenderer.endWidth =
            cutLineWidth;


        yield return
            new WaitForSeconds(
                0.12f
            );


        lineRenderer.enabled =
            false;


        lineRenderer.startWidth =
            normalLineWidth;

        lineRenderer.endWidth =
            normalLineWidth;
    }
}