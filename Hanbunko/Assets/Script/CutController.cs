using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CutController : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Renderer foodRenderer;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject retryButton;

    private Camera mainCamera;

    private Vector2 startPoint;
    private Vector2 endPoint;

    private bool isDragging = false;
    private bool hasCut = false;

    void Start()
    {
        mainCamera = Camera.main;

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        // 起動時は結果とRETRYボタンを隠す
        resultText.gameObject.SetActive(false);
        retryButton.SetActive(false);
    }

    void Update()
    {
        // 一度切ったらRETRYするまで切れない
        if (hasCut)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

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

    void StartCut(Vector2 screenPosition)
    {
        isDragging = true;
        lineRenderer.enabled = true;

        startPoint = ScreenToWorld(screenPosition);
        endPoint = startPoint;

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    void UpdateCut(Vector2 screenPosition)
    {
        endPoint = ScreenToWorld(screenPosition);

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    void EndCut(Vector2 screenPosition)
    {
        endPoint = ScreenToWorld(screenPosition);

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        isDragging = false;

        // Foodを端から端まで横切ったか確認
        if (LineCrossesFood(startPoint, endPoint))
        {
            hasCut = true;

            CalculateAreaRatio();
        }
        else
        {
            Debug.Log("Foodを端から端まで切ってください");

            lineRenderer.enabled = false;
        }
    }

    void CalculateAreaRatio()
    {
        Bounds bounds = foodRenderer.bounds;

        // Foodの四隅
        List<Vector2> foodPolygon = new List<Vector2>
        {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.min.x, bounds.max.y)
        };

        // 切断線の両側に分ける
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

        // それぞれの面積
        float areaA = CalculatePolygonArea(sideA);
        float areaB = CalculatePolygonArea(sideB);

        float totalArea = areaA + areaB;

        if (totalArea <= 0)
            return;

        // パーセントに変換
        float percentA =
            areaA / totalArea * 100f;

        float percentB =
            areaB / totalArea * 100f;

        string result =
            percentA.ToString("F1") + "% : " +
            percentB.ToString("F1") + "%";

        // 50%からどれくらい離れているか
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

        // 結果とRETRYボタンを表示
        resultText.gameObject.SetActive(true);
        retryButton.SetActive(true);

        resultText.text =
            result +
            "\n" +
            rank;

        Debug.Log(
            "結果：" +
            result +
            " / " +
            rank
        );
    }

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
            Vector2 current = polygon[i];

            Vector2 next =
                polygon[(i + 1) % polygon.Count];

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
                    ? currentSide >= 0
                    : currentSide <= 0;

            bool nextInside =
                keepPositive
                    ? nextSide >= 0
                    : nextSide <= 0;

            if (currentInside)
            {
                result.Add(current);
            }

            // 切断線との交点
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

    Vector2 GetLineIntersection(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d)
    {
        Vector2 r = b - a;
        Vector2 s = d - c;

        float cross =
            r.x * s.y -
            r.y * s.x;

        if (Mathf.Approximately(cross, 0))
        {
            return a;
        }

        Vector2 difference = c - a;

        float t =
            (
                difference.x * s.y -
                difference.y * s.x
            )
            / cross;

        return a + t * r;
    }

    float CalculatePolygonArea(
        List<Vector2> polygon)
    {
        if (polygon.Count < 3)
            return 0f;

        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = polygon[i];

            Vector2 next =
                polygon[(i + 1) % polygon.Count];

            area +=
                current.x * next.y -
                next.x * current.y;
        }

        return Mathf.Abs(area) * 0.5f;
    }

    bool LineCrossesFood(
        Vector2 start,
        Vector2 end)
    {
        Bounds bounds = foodRenderer.bounds;

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
            0))
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
            )
            / denominator;

        float u =
            (
                (c.x - a.x) *
                (b.y - a.y)
                -
                (c.y - a.y) *
                (b.x - a.x)
            )
            / denominator;

        return
            t >= 0 &&
            t <= 1 &&
            u >= 0 &&
            u <= 1;
    }

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

    // RETRYボタンから呼ばれる
    public void Retry()
    {
        lineRenderer.enabled = false;

        resultText.text = "";

        resultText.gameObject.SetActive(false);
        retryButton.SetActive(false);

        isDragging = false;
        hasCut = false;
    }
}