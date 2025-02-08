using UnityEngine;
using UnityEngine;
using System.Collections.Generic;


public class HypersphereWithRotatingVector : MonoBehaviour
{
    public int resolution = 32; // Higher resolution for smoother visualization
    public float radius = 5f; // Radius of the hypersphere
    public float rotationSpeed = 1f; // Base speed of rotation

    // Independent rotation speeds for each plane
    public float rotationXY = 1f;
    public float rotationXZ = 1f;
    public float rotationXW = 1f;
    public float rotationYZ = 1f;
    public float rotationYW = 1f;
    public float rotationZW = 1f;

    private List<Vector4> vertices = new List<Vector4>(); // 4D vertices of the hypersphere
    private GameObject[] pointObjects; // GameObjects representing the vertices
    private LineRenderer[] edges; // LineRenderers for edges

    private LineRenderer vectorRenderer; // LineRenderer for the vector
    private Vector4 vectorEndpoint; // Endpoint of the vector in 4D space

    void Start()
    {
        GenerateSmoothHyperspherePoints();
        CreatePointObjects();
        CreateEdges();
        CreateVectorRenderer();

        // Initialize the vector endpoint (normalized to lie on the hypersphere)
        vectorEndpoint = new Vector4(1, 1, 1, 1).normalized * radius;
    }

    void Update()
    {
        RotateVertices(Time.deltaTime);
        RotateVector(Time.deltaTime);
        UpdatePointObjects();
        UpdateEdges();
        DrawVector();
    }

    void GenerateSmoothHyperspherePoints()
    {
        vertices.Clear();
        for (int i = 0; i < resolution; i++)
        {
            float theta = Mathf.PI * 2 * i / resolution; // Angle around the x-y plane

            for (int j = 0; j < resolution / 2; j++) // Reduce density along latitude to avoid clustering
            {
                float phi = Mathf.PI * j / (resolution / 2); // Latitude angle

                for (int k = 0; k < resolution; k++)
                {
                    float psi = Mathf.PI * 2 * k / resolution; // Angle for z-w plane

                    // Generate 4D coordinates (x, y, z, w)
                    float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
                    float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
                    float z = radius * Mathf.Cos(phi) * Mathf.Cos(psi);
                    float w = radius * Mathf.Cos(phi) * Mathf.Sin(psi);

                    vertices.Add(new Vector4(x, y, z, w));
                }
            }
        }
    }

    void CreatePointObjects()
    {
        pointObjects = new GameObject[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.transform.localScale = Vector3.one * 0.1f;  // Small point size
            point.transform.parent = transform;
            pointObjects[i] = point;
        }
    }

    void CreateEdges()
    {
        int estimatedEdges = vertices.Count * 2;
        edges = new LineRenderer[estimatedEdges];

        for (int i = 0; i < edges.Length; i++)
        {
            GameObject edgeObject = new GameObject($"Edge_{i}");
            edgeObject.transform.parent = transform;

            LineRenderer lineRenderer = edgeObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            edges[i] = lineRenderer;
        }
    }

    void CreateVectorRenderer()
    {
        GameObject vectorObject = new GameObject("VectorRenderer");
        vectorObject.transform.parent = transform;

        vectorRenderer = vectorObject.AddComponent<LineRenderer>();
        vectorRenderer.material = new Material(Shader.Find("Sprites/Default"));
        vectorRenderer.startWidth = 0.05f;
        vectorRenderer.endWidth = 0.05f;
        vectorRenderer.positionCount = 2;

        // Set the color to green
        vectorRenderer.startColor = Color.green;
        vectorRenderer.endColor = Color.green;
    }

    void RotateVector(float deltaTime)
    {
        // Apply the same rotation matrices to the vector endpoint
        Matrix4x4 rotationMatrix = Matrix4x4.identity;

        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXY(rotationXY * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXZ(rotationXZ * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXW(rotationXW * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixYZ(rotationYZ * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixYW(rotationYW * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixZW(rotationZW * deltaTime * rotationSpeed));

        // Rotate and re-normalize the vector to stay on the hypersphere
        vectorEndpoint = rotationMatrix * vectorEndpoint;
        vectorEndpoint = vectorEndpoint.normalized * radius;
    }

    void DrawVector()
    {
        Vector3 projectedStart = ProjectTo3D(new Vector4(0, 0, 0, 0));
        Vector3 projectedEnd = ProjectTo3D(vectorEndpoint);

        vectorRenderer.SetPosition(0, projectedStart);
        vectorRenderer.SetPosition(1, projectedEnd);
    }

    void UpdatePointObjects()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            // Project the 4D vertex into 3D space
            Vector3 projectedPosition = ProjectTo3D(vertices[i]);
            pointObjects[i].transform.localPosition = projectedPosition;

            // Dynamically scale the point size based on the w-coordinate
            float sizeFactor = (vertices[i].w / radius + 1f) / 2f;
            pointObjects[i].transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.3f, sizeFactor);

            // Color encoding based on the w-coordinate
            pointObjects[i].GetComponent<Renderer>().material.color = Color.Lerp(Color.blue, Color.red, sizeFactor);
        }
    }

    void UpdateEdges()
    {
        int edgeIndex = 0;
        for (int i = 0; i < vertices.Count - 1; i++)
        {
            // Connect each point to its next neighbor (simplistic for demo purposes)
            if (edgeIndex < edges.Length)
                SetEdge(edges[edgeIndex++], vertices[i], vertices[(i + 1) % vertices.Count]);
        }
    }

    void SetEdge(LineRenderer line, Vector4 v1, Vector4 v2)
    {
        Vector3 p1 = ProjectTo3D(v1);
        Vector3 p2 = ProjectTo3D(v2);

        line.positionCount = 2;
        line.SetPosition(0, p1);
        line.SetPosition(1, p2);
    }

    void RotateVertices(float deltaTime)
    {
        // Create rotation matrices for each 4D plane
        Matrix4x4 rotationMatrix = Matrix4x4.identity;

        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXY(rotationXY * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXZ(rotationXZ * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixXW(rotationXW * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixYZ(rotationYZ * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixYW(rotationYW * deltaTime * rotationSpeed));
        rotationMatrix = MultiplyMatrices(rotationMatrix, GetRotationMatrixZW(rotationZW * deltaTime * rotationSpeed));

        // Apply the combined rotation matrix to all vertices and re-normalize
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rotationMatrix * vertices[i];
            vertices[i] = vertices[i].normalized * radius;  // Re-normalize to maintain the hypersphere radius
        }
    }

    Vector3 ProjectTo3D(Vector4 v)
    {
        // Enhanced projection that maps the w-coordinate dynamically
        float perspectiveFactor = 1f / (1f + v.w / radius);
        return new Vector3(v.x * perspectiveFactor, v.y * perspectiveFactor, v.z * perspectiveFactor);
    }

    Matrix4x4 GetRotationMatrixXY(float angle) => Create2DPlaneRotation(0, 1, angle);
    Matrix4x4 GetRotationMatrixXZ(float angle) => Create2DPlaneRotation(0, 2, angle);
    Matrix4x4 GetRotationMatrixXW(float angle) => Create2DPlaneRotation(0, 3, angle);
    Matrix4x4 GetRotationMatrixYZ(float angle) => Create2DPlaneRotation(1, 2, angle);
    Matrix4x4 GetRotationMatrixYW(float angle) => Create2DPlaneRotation(1, 3, angle);
    Matrix4x4 GetRotationMatrixZW(float angle) => Create2DPlaneRotation(2, 3, angle);

    Matrix4x4 Create2DPlaneRotation(int a, int b, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix[a, a] = cos; matrix[a, b] = -sin;
        matrix[b, a] = sin; matrix[b, b] = cos;
        return matrix;
    }

    Matrix4x4 MultiplyMatrices(Matrix4x4 a, Matrix4x4 b) => a * b;
}
