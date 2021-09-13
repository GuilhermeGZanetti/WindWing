using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class TesteMovimento : MonoBehaviour
{
    Rigidbody rb;

    private float targetAngleHorizontal;
    private float rotationVertical;
    private float verticalTorque;
    private float horizontalTorque;
    private Vector3 airForce;
    private Vector3 liftForce;

    public bool bateu = false;   
    public float liftModule = 0.5f;
    public bool asaFechada = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0, 0, 10);
        asaFechada = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Bate asas
        //if (Input.GetKeyDown(KeyCode.Space)){
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began){
            if(!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)){
                rb.AddRelativeForce(new Vector3(0, 0.5f, 1.5f), ForceMode.Impulse);
                //Debug.Log("Bateu asas!");
            } else {
                //Abriu ou Fechou asas
                if(asaFechada){
                    //Abre as asas
                    liftModule *= 5f;
                    asaFechada = false;
                } else {
                    //Fecha as asas
                    liftModule /= 5f;
                    asaFechada = true;
                }
            }
        }

        //Desenha a velocidade
        Debug.DrawLine(rb.position, rb.position+rb.velocity.normalized, Color.white);
        //Desenha a força
        Debug.DrawLine(rb.position, rb.position + airForce, Color.red);
        //Desenha o lift
        Debug.DrawLine(rb.position, rb.position + transform.TransformDirection(liftForce), Color.blue);
    }


    private void FixedUpdate() {
        float forwardVelocity = transform.InverseTransformDirection(rb.velocity).z;

        //Leitura do controle
        //Sideways rotation
        float rotationHorizontal = Input.acceleration.x;
        //rotationHorizontal = -4*Input.GetAxis("Horizontal");
        targetAngleHorizontal = -rotationHorizontal * 90f / 0.98f; //Convert from m/s2 to degree       

        //Vertical rotation
        rotationVertical = Input.acceleration.z;
        //rotationVertical = Input.GetAxis("Vertical");
        rotationVertical = (-rotationVertical - 0.7f) * 90f; //Convert from m/s2 to the angle
        // Find torque to get to target rotation      
        verticalTorque = rotationVertical / 300f;
        if(verticalTorque > 0){
            verticalTorque = 2f*verticalTorque;
        }

        ////// Controle direto do pássaro ////
        ControlHorizontalRotation();
        rb.AddRelativeTorque(Vector3.right * verticalTorque * Time.fixedDeltaTime); 



        //Faz com que a direção do rb siga a velocidade
        Vector3 deviance = (rb.velocity.normalized - transform.forward);
        //Alinha a direção do pássaro com a da velocidade
        rb.AddRelativeTorque(Vector3.up * transform.InverseTransformDirection(deviance).x * Time.fixedDeltaTime / 2); // (forwardVelocity/20));
        rb.AddRelativeTorque(Vector3.right * transform.InverseTransformDirection(deviance).y * Time.fixedDeltaTime / -2);// (-forwardVelocity/20));

        //E o contrário. Joga a velocidade mais para a direção do pássaro
        airForce = -deviance * 500f * Time.fixedDeltaTime * Mathf.Abs(forwardVelocity)/6;
        rb.AddForce(airForce); //, ForceMode.VelocityChange);
        

        //gravidade
        rb.AddForce(new Vector3(0, -5f, 0));
        //Sustentação
        float lift = Mathf.Abs(forwardVelocity) * liftModule - (verticalTorque * Mathf.Abs(forwardVelocity) * liftModule);
        Debug.Log("Vertical Torque: " + verticalTorque + " // Lift: " + lift);
        liftForce = new Vector3(0, lift, 0); //forwardVelocity * forwardVelocity * 0.06f //Mathf.Abs(forwardVelocity)*0.5f
        rb.AddForce(transform.TransformDirection(liftForce));
        Debug.Log("Lift: " + liftForce.y +" // Foward Velocity: " + forwardVelocity + " // airForce: " + airForce.magnitude +" // Deviance: " + transform.InverseTransformDirection(deviance));
        //Debug.Log("Velocity: " + rb.velocity);

        //Arrasto
        float dragForce = forwardVelocity*forwardVelocity * 0.003f;
        rb.AddRelativeForce(new Vector3(0, 0, -dragForce));


        
    }

    void OnCollisionEnter(Collision collision)
    {
        bateu = true;
        Application.LoadLevel(Application.loadedLevel);
    }

    
    private void ControlHorizontalRotation(){
        //(0, 1, 0) -> vetor unitário ortogonal ao plano global x z
        //Encontrar angulo entre transform.up e vetor ortogonal a xz
        float currentAngle = Vector3.Angle(Vector3.up, transform.up) * Mathf.Sign(transform.right.y);
        float angularVelocityLocalZ = transform.InverseTransformDirection(rb.angularVelocity).z;
        //Debug.Log("Alvo: "+targetAngleHorizontal + " Angulação atual: " + currentAngle + " Torque: "+ (targetAngleHorizontal - currentAngle) + " W: " + angularVelocityLocalZ);
        //Encontrar torque subtraindo o angulo atual pelo angulo alvo
        //horizontalTorque = Mathf.Sign(targetAngleHorizontal - currentAngle) * Mathf.Pow(targetAngleHorizontal - currentAngle, 2) /5000f;
        horizontalTorque = (targetAngleHorizontal - currentAngle) / 10f;
        //Freiar virada
        //Alvo à direita (negativa)
        if(targetAngleHorizontal - currentAngle < 0){
                if(angularVelocityLocalZ < (targetAngleHorizontal - currentAngle)/20f){
                    horizontalTorque = -angularVelocityLocalZ * 10;
                    //Debug.Log("Freiando! "+horizontalTorque);
                }
        }
        //Alvo à esquerda (positiva)
            if(targetAngleHorizontal - currentAngle > 0){
                if(angularVelocityLocalZ > (targetAngleHorizontal - currentAngle)/20f){
                    horizontalTorque = -angularVelocityLocalZ * 10;
                    //Debug.Log("Freiando! "+horizontalTorque);
                }
        }

        rb.AddRelativeTorque(Vector3.forward * horizontalTorque * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }
}



