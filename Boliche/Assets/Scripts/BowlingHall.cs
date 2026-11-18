using UnityEngine;

public sealed class BowlingHall
{
    internal const float LaneWidth = 1.054f;
    internal const float LaneLength = 12f;
    internal const float HeadPin = LaneLength - 1.412f;
    internal const float BallPit = LaneLength + .55f;
    private static readonly Color Midnight = new Color(.035f, .055f, .09f);
    private readonly Transform root;
    private readonly Material wood, wall, inset, brass, white, red, ballFinish, amber, teal;
    private readonly PhysicsMaterial laneContact, ballContact, pinContact;
    private readonly Mesh pinMesh;

    public BowlingHall(Transform root, Shader shader, GameObject laneModel)
    {
        this.root = root;
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        wood = Finish(shader, new Color(.68f, .39f, .19f), .42f);
        wall = Finish(shader, Midnight, .25f);
        inset = Finish(shader, new Color(.08f, .14f, .20f), .42f);
        brass = Finish(shader, new Color(.78f, .47f, .20f), .58f);
        white = Finish(shader, new Color(.94f, .92f, .84f), .42f);
        red = Finish(shader, new Color(.64f, .035f, .05f), .47f);
        ballFinish = Finish(shader, new Color(.02f, .33f, .31f), .78f);
        amber = Glow(shader, new Color(1f, .62f, .26f));
        teal = Glow(shader, new Color(.12f, .68f, .62f));
        laneContact = Contact("Madeira envernizada", .24f, .08f);
        ballContact = Contact("Resina da bola", .22f, .14f);
        pinContact = Contact("Revestimento dos pinos", .38f, .23f);
        pinMesh = CreatePinMesh();
        Build(TryPlaceOriginalModel(laneModel, shader));
    }

    private void Build(bool useOriginalModel)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.48f, .52f, .58f);

        Box("Pista", new Vector3(0, -.06f, LaneLength / 2), new Vector3(LaneWidth, .12f, LaneLength),
            useOriginalModel ? null : wood, laneContact);
        Box("Área de aproximação", new Vector3(0, -.07f, -2.6f), new Vector3(3.9f, .14f, 5.2f),
            useOriginalModel ? null : wood, laneContact);
        foreach (int side in new[] { -1, 1 })
        {
            Box("Canaleta", new Vector3(side * .71f, -.202f, (LaneLength + .1f) / 2), new Vector3(.365f, .10f, LaneLength + .1f),
                useOriginalModel ? null : inset, laneContact);
            Box("Guia externa", new Vector3(side * .91f, -.067f, (LaneLength + .1f) / 2), new Vector3(.06f, .15f, LaneLength + .1f),
                useOriginalModel ? null : brass, laneContact);
        }
        Box("Poço dos pinos", new Vector3(0, -.35f, LaneLength + .75f), new Vector3(1.9f, .12f, 1.5f),
            useOriginalModel ? null : wall, laneContact);

        if (!useOriginalModel)
            BuildFallbackScenery();

        Lamp(LightType.Directional, "Luz ambiente", Vector3.zero, new Vector3(55, -25, 0), new Color(1, .88f, .73f), 1.05f, 0f);
        Lamp(LightType.Point, "Luz dos pinos", new Vector3(0, 2.47f, HeadPin + .362f), Vector3.zero, new Color(.87f, .96f, 1), 2.5f, 6f);
        Lamp(LightType.Point, "Luz de aproximação", new Vector3(0, 2.7f, -.9f), Vector3.zero, new Color(1, .8f, .6f), 1.7f, 7f);
    }

    private bool TryPlaceOriginalModel(GameObject prefab, Shader shader)
    {
        if (prefab == null)
            return false;

        Transform alignment = new GameObject("Modelo original da pista").transform;
        alignment.SetParent(root, false);
        GameObject model = Object.Instantiate(prefab, alignment);
        model.transform.localRotation = Quaternion.Euler(0, -90f, 0);
        MeshRenderer lane = model.transform.Find("lane")?.GetComponent<MeshRenderer>();
        MeshRenderer approach = model.transform.Find("approach")?.GetComponent<MeshRenderer>();
        MeshRenderer pin = model.transform.Find("pin")?.GetComponent<MeshRenderer>();
        if (lane == null || approach == null || pin == null)
        {
            alignment.gameObject.SetActive(false);
            Object.Destroy(alignment.gameObject);
            return false;
        }

        if (approach.bounds.center.z > lane.bounds.center.z)
            model.transform.localRotation = Quaternion.Euler(0, 90f, 0);

        Bounds bounds = lane.bounds;
        // The FBX meshes contain both lanes. This fraction is the width of the lane with the sample pin.
        const float singleLaneFraction = 2.856f / 10.8795f;
        float originalLaneWidth = bounds.size.x * singleLaneFraction;
        bool atLeftEdge = pin.bounds.center.x < bounds.center.x;
        float centerX = atLeftEdge ? bounds.min.x + originalLaneWidth / 2 : bounds.max.x - originalLaneWidth / 2;
        var scale = new Vector3(LaneWidth / originalLaneWidth, LaneWidth / originalLaneWidth,
            LaneLength / bounds.size.z);
        var foulLine = new Vector3(centerX, bounds.max.y, bounds.min.z);
        alignment.localScale = scale;
        alignment.localPosition = -Vector3.Scale(foulLine, scale);

        var materials = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            switch (renderer.gameObject.name)
            {
                case "pin":
                case "pin_s_":
                case "pin_dots":
                case "venom_ball":
                case "venom_fingers":
                    // The playable ball and rack are driven by rigidbodies.
                    renderer.enabled = false;
                    continue;
                case "structure1":
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    break;
            }

            Material[] resolved = renderer.sharedMaterials;
            for (int i = 0; i < resolved.Length; i++)
            {
                Material imported = resolved[i];
                if (imported == null)
                    continue;
                if (!materials.TryGetValue(imported, out Material adapted))
                {
                    Texture texture = imported.mainTexture;
                    if (texture == null)
                        texture = Resources.Load<Texture2D>("bowling-lane/textures/" + imported.name + "_TXTR");
                    Color tint = texture == null && imported.HasProperty("_Color") ? imported.color : Color.white;
                    adapted = Finish(shader, tint, .3f);
                    adapted.mainTexture = texture;
                    materials.Add(imported, adapted);
                }
                resolved[i] = adapted;
            }
            renderer.sharedMaterials = resolved;
        }
        return true;
    }

    private void BuildFallbackScenery()
    {
        const float roomLength = LaneLength + 7.30f;
        const float roomCenter = LaneLength / 2 - 1.85f;
        const float sectionLength = roomLength / 7.4f;
        Box("Linha de falta", new Vector3(0, .0008f, .016f), new Vector3(LaneWidth, .001f, .028f), wall);

        for (int board = 0; board < 39; board++)
        {
            float x = LaneWidth * ((board + .5f) / 39 - .5f);
            float tint = .88f + (board * 29 % 13) * .016f;
            Material plank = Finish(wood.shader, wood.color * tint, .53f);
            Box("Tábua", new Vector3(x, .00025f, LaneLength / 2), new Vector3(.0268f, .0005f, LaneLength), plank);
        }

        for (int i = -3; i <= 3; i++)
        {
            float z = LaneLength * .24f - Mathf.Abs(i) * .18f;
            Box("Marcação de mira", new Vector3(i * .127f, .001f, z), new Vector3(.018f, .001f, .065f), wall);
            Box("Referência de saída", new Vector3(i * .127f, .001f, -.83f), new Vector3(.014f, .001f, .014f), wall);
        }

        foreach (int side in new[] { -1, 1 })
        {
            Box("Piso lateral", new Vector3(side * 1.46f, -.12f, (LaneLength + .1f) / 2), new Vector3(1.05f, .14f, LaneLength + .1f), inset);
            Box("Parede lateral", new Vector3(side * 2f, 1.5f, roomCenter), new Vector3(.16f, 3.2f, roomLength), wall);
            Box("Luz lateral", new Vector3(side * 1.905f, .55f, roomCenter), new Vector3(.018f, .026f, roomLength - 2), teal);

            for (int section = 0; section < 7; section++)
            {
                float z = -3f + section * sectionLength;
                Box("Painel acústico", new Vector3(side * 1.90f, 1.83f, z), new Vector3(.05f, 1.50f, sectionLength * .9f), inset);
            }
        }

        Box("Fundo", new Vector3(0, 1.45f, LaneLength + 1.52f), new Vector3(4f, 3.4f, .18f), wall);
        Box("Painel da pista", new Vector3(0, 1.88f, LaneLength + .30f), new Vector3(3.8f, 1.06f, .18f), inset);
        Box("Friso do painel", new Vector3(0, 1.4f, LaneLength + .19f), new Vector3(3.75f, .03f, .025f), amber);
        Box("Teto", new Vector3(0, 3.15f, roomCenter), new Vector3(4f, .12f, roomLength), wall, castShadows: false);

        for (int i = 0; i < 7; i++)
        {
            float z = -2.7f + i * sectionLength;
            Box("Luminária", new Vector3(0, 3.06f, z), new Vector3(2.35f, .03f, .07f), amber);
        }
    }

    public Rigidbody CreateBall()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Bola • 6,8 kg";
        sphere.transform.SetParent(root);
        sphere.transform.localScale = Vector3.one * .217f;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = ballFinish;
        sphere.GetComponent<SphereCollider>().sharedMaterial = ballContact;
        return Body(sphere, 6.8f);
    }

    public Rigidbody CreatePin(int number, Vector3 position)
    {
        GameObject pin = new GameObject($"Pino {number}", typeof(MeshFilter), typeof(MeshRenderer), typeof(Pin));
        pin.transform.SetParent(root);
        pin.transform.position = position;
        pin.GetComponent<MeshFilter>().sharedMesh = pinMesh;
        pin.GetComponent<MeshRenderer>().sharedMaterials = new[] { white, red };
        BoxCollider baseCollider = pin.AddComponent<BoxCollider>();
        baseCollider.center = Vector3.up * .012f;
        baseCollider.size = new Vector3(.060f, .024f, .060f);
        baseCollider.sharedMaterial = pinContact;
        Capsule(pin, .075f, .068f, .147f);
        Capsule(pin, .172f, .052f, .125f);
        Capsule(pin, .245f, .027f, .120f);
        Capsule(pin, .332f, .041f, .098f);
        Rigidbody body = Body(pin, 1.56f);
        body.centerOfMass = Vector3.up * .136f;
        return body;
    }

    private void Capsule(GameObject pin, float height, float radius, float length)
    {
        CapsuleCollider shape = pin.AddComponent<CapsuleCollider>();
        shape.center = Vector3.up * height;
        shape.radius = radius;
        shape.height = length;
        shape.sharedMaterial = pinContact;
    }

    private static Rigidbody Body(GameObject item, float mass)
    {
        Rigidbody body = item.AddComponent<Rigidbody>();
        body.mass = mass;
        body.linearDamping = .045f;
        body.angularDamping = .19f;
        body.maxAngularVelocity = 100f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 12;
        return body;
    }

    private void Box(string name, Vector3 position, Vector3 scale, Material material, PhysicsMaterial contact = null, bool castShadows = true)
    {
        GameObject box = material == null ? new GameObject(name, typeof(BoxCollider)) :
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root);
        box.transform.SetPositionAndRotation(position, Quaternion.identity);
        box.transform.localScale = scale;
        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            if (!castShadows)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Collider collider = box.GetComponent<Collider>();
        collider.enabled = contact != null;
        collider.sharedMaterial = contact;
    }

    private void Lamp(LightType type, string name, Vector3 position, Vector3 rotation, Color color, float intensity, float range)
    {
        Light lamp = new GameObject(name, typeof(Light)).GetComponent<Light>();
        lamp.transform.SetParent(root);
        lamp.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        lamp.type = type;
        lamp.color = color;
        lamp.intensity = intensity;
        lamp.range = range;
        if (type == LightType.Directional)
            lamp.shadows = LightShadows.Soft;
    }

    private static Material Finish(Shader shader, Color color, float smoothness)
    {
        Material material = new Material(shader) { color = color };
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Material Glow(Shader shader, Color color)
    {
        Material material = Finish(shader, color, .45f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 1.7f);
        return material;
    }

    private static PhysicsMaterial Contact(string name, float friction, float bounce)
    {
        return new PhysicsMaterial(name)
        {
            staticFriction = friction,
            dynamicFriction = friction * .77f,
            bounciness = bounce
        };
    }

    private static Mesh CreatePinMesh()
    {
        Vector2[] profile =
        {
            new Vector2(0, .037f), new Vector2(.014f, .052f), new Vector2(.047f, .064f),
            new Vector2(.090f, .061f), new Vector2(.132f, .050f), new Vector2(.185f, .036f),
            new Vector2(.225f, .026f), new Vector2(.238f, .024f), new Vector2(.249f, .024f),
            new Vector2(.260f, .025f), new Vector2(.271f, .026f), new Vector2(.296f, .032f),
            new Vector2(.325f, .041f), new Vector2(.349f, .038f), new Vector2(.371f, .024f),
            new Vector2(.381f, .001f)
        };

        const int sides = 32;
        var vertices = new Vector3[profile.Length * sides];
        var body = new System.Collections.Generic.List<int>();
        var stripes = new System.Collections.Generic.List<int>();
        for (int ring = 0; ring < profile.Length; ring++)
        for (int side = 0; side < sides; side++)
        {
            float angle = side * Mathf.PI * 2f / sides;
            int a = ring * sides + side;
            vertices[a] = new Vector3(Mathf.Cos(angle) * profile[ring].y, profile[ring].x,
                Mathf.Sin(angle) * profile[ring].y);
            if (ring == profile.Length - 1)
                continue;
            int b = ring * sides + (side + 1) % sides;
            var surface = ring == 7 || ring == 9 ? stripes : body;
            surface.Add(a);
            surface.Add(a + sides);
            surface.Add(b);
            surface.Add(b);
            surface.Add(a + sides);
            surface.Add(b + sides);
        }

        Mesh mesh = new Mesh { name = "Perfil torneado do pino", vertices = vertices, subMeshCount = 2 };
        mesh.SetTriangles(body, 0);
        mesh.SetTriangles(stripes, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
