using UnityEngine;

public class JellyController : MonoBehaviour
{

    [SerializeField] private JellyColor jellyColor;
    [SerializeField] private BulletController bulletController;
    [SerializeField] private Vector2 aimDirection = Vector2.up;
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private Transform boardCenter;
    //private float fireTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //InvokeRepeating(nameof(Fire), 0f, fireInterval);

    }

    void Fire()
    {
        Vector2 direction = GetAimDirection();
        BulletController bullet = Instantiate(bulletController, transform.position, Quaternion.identity);
        bullet.Init(direction, jellyColor);
    }

    // Update is called once per frame
    void Update()
    {
        //fireTimer += Time.deltaTime;
        //if (fireTimer >= fireInterval)
        //{
        //    fireTimer = 0f;
        //    Fire();
        //}

    }

    Vector2 GetAimDirection()
    {
        Vector2 offset = transform.position - boardCenter.position;
        if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
            return offset.x > 0 ? Vector2.left : Vector2.right;
        else
            return offset.y > 0 ? Vector2.down : Vector2.up;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Fire();
        return;
    }
}
