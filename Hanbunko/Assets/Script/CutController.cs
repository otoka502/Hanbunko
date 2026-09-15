using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CutController : MonoBehaviour
{
    [Header("äÓñ{ê›íË")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Renderer foodRenderer;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject nextButton;

    [Header("NEXTÇ…êiÇﬂÇÈï]âø")]
    [SerializeField] private bool perfectCanNext = true;
    [SerializeField] private bool greatCanNext = true;
    [SerializeField] private bool goodCanNext = true;
    [SerializeField] private bool tryAgainCanNext = false;

    [Header("êÿífââèo")]
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

    void Start()
    {
        mainCamera = Camera.main;

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        lineRenderer.startWidth = normalLineWidth;
        lineRenderer.endWidth = normalLineWidth;

        resultText.gameObject.SetActive(false);
        retryButton.SetActive(false);
        nextButton.SetActive(false);
    }

    void Update()
    {
        if (hasCut)
            return;

        // É^ÉbÉ`ëÄçÏ
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

        // É}ÉEÉXëÄçÏ
        if (Mouse.current != null)
        {
            HandleMouse();
        }
    }

    void HandleMouse()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCut(mousePosition);
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            UpdateCut(mousePosition);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            EndCut(mousePosition);
        }
    }

    void HandleTouch()
    {
        var touch =
            Touchscreen.current.primaryTouch;

        Vector2 touchPosition =
            touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            StartCut(touchPosition);
        }

        if (touch.press.isPressed && isDragging)
        {
            UpdateCut(touchPosition);
        }

        if (touch.press.wasReleasedThisFrame && isDragging)
        {
            EndCut(touchPosition);
        }
    }

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
            Debug.Log("FoodÇí[Ç©ÇÁí[Ç‹Ç≈êÿÇ¡ÇƒÇ≠ÇæÇ≥Ç¢");

            lineRenderer.enabled = false;
            return;
        }

        hasCut = true;

        CalculateAreaRatio();
        SplitFood();

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

    // ============================================
    // ñ êœåvéZ
    // ============================================

    void CalculateAreaRatio()
    {
        List<Vector2> foodPolygon =
            GetFoodPolygon();

        List<Vector2> sideA =
            ClipPolygon(
                foodPolygon,
                startPoint,
                endPoint,
                true
            );

        List<Vector2> sideB =
            ClipPolygon(
                foodPolygon,
                startPoint,
                endPoint,
                false
            );

        float areaA =
            CalculatePolygonArea(sideA);

        float areaB =
            CalculatePolygonArea(sideB);

        float totalArea =
            areaA + areaB;

        if (totalArea <= 0f)
            return;

        float percentA =
            areaA / totalArea * 100f;

        float percentB =
            areaB / totalArea * 100f;

        string result =
            percentA.ToString("F1") +
            "% : " +
            percentB.ToString("F1") +
            "%";

        float difference =
            Mathf.Abs(percentA - 50f);

        string rank;

        if (difference <= 1f)
        {
            rank = "PERFECT";
        }
        else if (difference <= 3f)
        {
            rank = "GREAT";
        }
        else if (difference <= 5f)
        {
            rank = "GOOD";
        }
        else
        {
            rank = "TRY AGAIN";
        }

        bool canNext = false;

        if (rank == "PERFECT")
            canNext = perfectCanNext;

        else if (rank == "GREAT")
            canNext = greatCanNext;

        else if (rank == "GOOD")
            canNext = goodCanNext;

        else if (rank == "TRY AGAIN")
            canNext = tryAgainCanNext;

        resultText.gameObject.SetActive(true);

        resultText.text =
            result +
            "\n" +
            rank;

        if (canNext)
        {
            retryButton.SetActive(false);
            nextButton.SetActive(true);
        }
        else
        {
            retryButton.SetActive(true);
            nextButton.SetActive(false);
        }

        Debug.Log(
            "åãâ ÅF" +
            result +
            " / " +
            rank
        );
    }

    // ============================================
    // FoodÇÃí∑ï˚å`
    // ============================================

    List<Vector2> GetFoodPolygon()
    {
        Bounds bounds =
            foodRenderer.bounds;

        return new List<Vector2>
        {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.min.x, bounds.max.y)
        };
    }

    // ============================================
    // FoodÇ2Ç¬Ç…ï™äÑ
    // ============================================

    void SplitFood()
    {
        List<Vector2> originalPolygon =
            GetFoodPolygon();

        List<Vector2> polygonA =
            ClipPolygon(
                originalPolygon,
                startPoint,
                endPoint,
                true
            );

        List<Vector2> polygonB =
            ClipPolygon(
                originalPolygon,
                startPoint,
                endPoint,
                false
            );

        if (polygonA.Count < 3 ||
            polygonB.Count < 3)
        {
            Debug.LogWarning(
                "ï™äÑÉ|ÉäÉSÉìÇê∂ê¨Ç≈Ç´Ç‹ÇπÇÒÇ≈ÇµÇΩ"
            );

            hasCut = false;
            return;
        }

        Material foodMaterial =
            foodRenderer.sharedMaterial;

        splitObjectA =
            CreateSplitObject(
                "Food_Split_A",
                polygonA,
                foodMaterial
            );

        splitObjectB =
            CreateSplitObject(
                "Food_Split_B",
                polygonB,
                foodMaterial
            );

        if (splitObjectA == null ||
            splitObjectB == null)
        {
            Debug.LogWarning(
                "ï™äÑMeshÇÃê∂ê¨Ç…é∏îsÇµÇ‹ÇµÇΩ"
            );

            hasCut = false;
            return;
        }

        // êÿífê¸ÇÃï˚å¸
        Vector2 cutDirection =
            (endPoint - startPoint).normalized;

        // êÿífê¸Ç…ëŒÇµÇƒêÇíº
        Vector2 separationDirection =
            new Vector2(
                -cutDirection.y,
                cutDirection.x
            );

        // AÇ™Ç«ÇøÇÁë§Ç…Ç†ÇÈÇ©í≤Ç◊ÇÈ
        Vector2 centerA =
            GetPolygonCenter(polygonA);

        float side =
            SideOfLine(
                startPoint,
                endPoint,
                centerA
            );

        if (side < 0f)
        {
            separationDirection *= -1f;
        }

        // å≥FoodÇè¡Ç∑
        foodRenderer.gameObject.SetActive(false);

        // ï™äÑï–Çó£Ç∑
        StartCoroutine(
            MoveSplitPieces(
                separationDirection
            )
        );
    }

    // ============================================
    // ï™äÑFoodê∂ê¨
    // ============================================

    GameObject CreateSplitObject(
        string objectName,
        List<Vector2> polygon,
        Material material)
    {
        GameObject obj =
            new GameObject(objectName);

        MeshFilter meshFilter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            obj.AddComponent<MeshRenderer>();

        Mesh mesh =
            CreateMeshFromPolygon(polygon);

        if (mesh == null)
        {
            Destroy(obj);
            return null;
        }

        meshFilter.mesh = mesh;

        meshRenderer.sharedMaterial =
            material;

        obj.layer =
            foodRenderer.gameObject.layer;

        return obj;
    }

    // ============================================
    // É|ÉäÉSÉì Å® Mesh
    // ============================================

    Mesh CreateMeshFromPolygon(
        List<Vector2> polygon)
    {
        if (polygon == null ||
            polygon.Count < 3)
        {
            return null;
        }

        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
            new Vector3[polygon.Count];

        Vector2[] uv =
            new Vector2[polygon.Count];

        Bounds bounds =
            foodRenderer.bounds;

        for (int i = 0; i < polygon.Count; i++)
        {
            vertices[i] =
                new Vector3(
                    polygon[i].x,
                    polygon[i].y,
                    bounds.center.z
                );

            float u =
                Mathf.InverseLerp(
                    bounds.min.x,
                    bounds.max.x,
                    polygon[i].x
                );

            float v =
                Mathf.InverseLerp(
                    bounds.min.y,
                    bounds.max.y,
                    polygon[i].y
                );

            uv[i] =
                new Vector2(u, v);
        }

        int triangleCount =
            polygon.Count - 2;

        int[] triangles =
            new int[
                triangleCount * 3
            ];

        // ÉJÉÅÉâë§Çå¸Ç≠ÇÊÇ§Ç…í∏ì_èáÇîΩì]
        for (int i = 0; i < triangleCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // ============================================
    // êÿífï–Çó£Ç∑
    // ============================================

    IEnumerator MoveSplitPieces(
        Vector2 direction)
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
            ) * splitDistance;

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
                    time / splitDuration
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

    // ============================================
    // É|ÉäÉSÉìíÜêS
    // ============================================

    Vector2 GetPolygonCenter(
        List<Vector2> polygon)
    {
        Vector2 center =
            Vector2.zero;

        for (int i = 0; i < polygon.Count; i++)
        {
            center += polygon[i];
        }

        center /= polygon.Count;

        return center;
    }

    // ============================================
    // É|ÉäÉSÉìÇê¸Ç≈êÿÇÈ
    // ============================================

    List<Vector2> ClipPolygon(
        List<Vector2> polygon,
        Vector2 lineStart,
        Vector2 lineEnd,
        bool keepPositive)
    {
        List<Vector2> result =
            new List<Vector2>();

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current =
                polygon[i];

            Vector2 next =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];

            float currentSide =
                SideOfLine(
                    lineStart,
                    lineEnd,
                    current
                );

            float nextSide =
                SideOfLine(
                    lineStart,
                    lineEnd,
                    next
                );

            bool currentInside =
                keepPositive
                    ? currentSide >= 0f
                    : currentSide <= 0f;

            bool nextInside =
                keepPositive
                    ? nextSide >= 0f
                    : nextSide <= 0f;

            if (currentInside)
            {
                result.Add(current);
            }

            if (currentInside != nextInside)
            {
                Vector2 intersection =
                    GetLineIntersection(
                        current,
                        next,
                        lineStart,
                        lineEnd
                    );

                result.Add(intersection);
            }
        }

        return result;
    }

    // ============================================
    // ì_Ç™ê¸ÇÃÇ«ÇøÇÁë§Ç©
    // ============================================

    float SideOfLine(
        Vector2 a,
        Vector2 b,
        Vector2 point)
    {
        return
            (b.x - a.x) *
            (point.y - a.y)
            -
            (b.y - a.y) *
            (point.x - a.x);
    }

    // ============================================
    // åì_
    // ============================================

    Vector2 GetLineIntersection(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d)
    {
        Vector2 r =
            b - a;

        Vector2 s =
            d - c;

        float cross =
            r.x * s.y -
            r.y * s.x;

        if (Mathf.Approximately(cross, 0f))
        {
            return a;
        }

        Vector2 difference =
            c - a;

        float t =
            (
                difference.x * s.y -
                difference.y * s.x
            ) / cross;

        return a + t * r;
    }

    // ============================================
    // ñ êœ
    // ============================================

    float CalculatePolygonArea(
        List<Vector2> polygon)
    {
        if (polygon.Count < 3)
            return 0f;

        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current =
                polygon[i];

            Vector2 next =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];

            area +=
                current.x * next.y -
                next.x * current.y;
        }

        return
            Mathf.Abs(area) * 0.5f;
    }

    // ============================================
    // Foodâ°ífîªíË
    // ============================================

    bool LineCrossesFood(
        Vector2 start,
        Vector2 end)
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
            hitCount++;

        if (LinesIntersect(
            start,
            end,
            topLeft,
            topRight))
            hitCount++;

        if (LinesIntersect(
            start,
            end,
            bottomLeft,
            topLeft))
            hitCount++;

        if (LinesIntersect(
            start,
            end,
            bottomRight,
            topRight))
            hitCount++;

        return hitCount >= 2;
    }

    // ============================================
    // ê¸ï™åç∑
    // ============================================

    bool LinesIntersect(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d)
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
            (
                (c.x - a.x) *
                (d.y - c.y)
                -
                (c.y - a.y) *
                (d.x - c.x)
            ) / denominator;

        float u =
            (
                (c.x - a.x) *
                (b.y - a.y)
                -
                (c.y - a.y) *
                (b.x - a.x)
            ) / denominator;

        return
            t >= 0f &&
            t <= 1f &&
            u >= 0f &&
            u <= 1f;
    }

    // ============================================
    // Screen Å® World
    // ============================================

    Vector2 ScreenToWorld(
        Vector2 screenPosition)
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

    // ============================================
    // êÿífê¸ââèo
    // ============================================

    IEnumerator CutLineEffect()
    {
        lineRenderer.startWidth =
            cutLineWidth;

        lineRenderer.endWidth =
            cutLineWidth;

        yield return
            new WaitForSeconds(0.12f);

        lineRenderer.enabled = false;

        lineRenderer.startWidth =
            normalLineWidth;

        lineRenderer.endWidth =
            normalLineWidth;
    }

    // ============================================
    // RETRY
    // ============================================

    public void Retry()
    {
        StopAllCoroutines();

        if (splitObjectA != null)
        {
            Destroy(splitObjectA);
            splitObjectA = null;
        }

        if (splitObjectB != null)
        {
            Destroy(splitObjectB);
            splitObjectB = null;
        }

        // å≥FoodÇïúäà
        foodRenderer.gameObject.SetActive(true);

        lineRenderer.enabled = false;
        lineRenderer.startWidth = normalLineWidth;
        lineRenderer.endWidth = normalLineWidth;

        resultText.text = "";
        resultText.gameObject.SetActive(false);

        retryButton.SetActive(false);
        nextButton.SetActive(false);

        isDragging = false;
        hasCut = false;
    }
}