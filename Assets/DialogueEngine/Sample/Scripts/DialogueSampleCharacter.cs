using DialogueEngine;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DialogueSampleCharacter : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 12f;
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private DialogueSample dialogueSample;

    private NavMeshAgent agent;
    private float fixedY;
    private Vector3 moveInput;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        fixedY = transform.position.y;
    }

    private void Update()
    {
        if (dialogueSample.IsDialogueActive)
        {
            return;
        }

        var inputX = Input.GetAxisRaw("Horizontal");
        var inputZ = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(inputX, 0f, inputZ);
        moveInput.Normalize();

        if (agent != null)
        {
            agent.Move(moveInput * (moveSpeed * Time.deltaTime));
        }

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            var targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryStartDialogueByRaycast();
        }
    }

    private void TryStartDialogueByRaycast()
    {
        var origin = transform.position + (Vector3.up * 0.5f);
        var direction = transform.forward;

        if (Physics.Raycast(origin, direction, out var hit, interactDistance))
        {
            var speaker = hit.collider.GetComponentInParent<DialogueSampleSpeaker>();
            if (speaker == null || string.IsNullOrWhiteSpace(speaker.DialogueId))
            {
                return;
            }

            DialogueEngineManager.StartDialogue(speaker.DialogueId);
        }
    }
}
