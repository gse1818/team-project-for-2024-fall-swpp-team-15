using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Yogaewonsil.Common;

public class CustomerNPC : MonoBehaviour
{
    public NavMeshAgent customerAgent;
    private Table assignedTable;
    private CustomerManager customerManager;

    public Vector3 spawnPosition; // 손님이 레스토랑을 떠날 위치
    private bool isSeated = false;
    private bool hasOrdered = false;
    private bool isEating = false;

    private FoodData orderedDish;
    public float patience = 90f; // 인내심 게이지
    private float patienceTimer;

    public Image patienceGauge; // 인내심 게이지 UI
    public Sprite orangeButton;
    private Button orderButton;

    [Header("Audio Settings")]
    [SerializeField] protected AudioSource audioSource; // 요리 사운드를 재생할 AudioSource
    [SerializeField] private AudioClip eatSound; // 요리 시작 시 재생할 사운드

    [Header("Satisfaction")]
    [SerializeField] private Texture HappyIcon; // 요리 사운드를 재생할 AudioSource
    [SerializeField] private Texture DisappointIcon; // 요리 시작 시 재생할 사운드

    /// <summary>
    /// 손님이 음식을 성공적으로 받았는지 여부.
    /// </summary>
    public bool isFoodReceived = false;

    [Header("Database")]
    [SerializeField] private FoodDatabaseSO foodDatabase; // 음식 데이터베이스

    void Start()
    {   
        // NavMeshAgent 설정 조정(충돌없이 서로 통과할 수 있도록)
        customerAgent.avoidancePriority = 0; // 가장 높은 우선순위로 경로를 확보
        customerAgent.radius = 0.1f; // 충돌 감지 반경 최소화 


        customerManager = GameObject.Find("CustomerManager").GetComponent<CustomerManager>();
        FindAndMoveToTable();
        patienceTimer = patience;

        orderButton = GetComponentInChildren<Button>();
        orderButton.gameObject.SetActive(false);
        orderButton.onClick.AddListener(OnOrderButtonClick);

        patienceGauge = transform.Find("Canvas/OrderButton/PatienceGauge").GetComponent<Image>();

        GetRandomDishFromCustomerManager();
    }

    void Update()
    {   
        if (hasOrdered && !isEating)
        {
            patienceTimer -= Time.deltaTime;
            UpdatePatienceGauge();

            if (patienceTimer <= 0)
            {
                HandleUnhappyExit(); // 인내심이 0이 되었을 때 퇴장
            }
            else if (assignedTable.plateFood != null)
            {   
                StartEating(); // 테이블에 음식이 있을 경우 확인
            }
        }

        CheckIfReachedTable();
    }

    private void UpdatePatienceGauge()
    {
        if (patienceGauge != null)
        {
            patienceGauge.fillAmount = patienceTimer / patience;
        }
    }

    private void HandleUnhappyExit()
    {
        Debug.Log("Customer left due to impatience.");
        // 주문목록에서 삭제
        customerManager.orderManager.RemoveOrder(this, orderedDish);
        DisplayIcon(DisappointIcon);
        ExitRestaurant();
    }

    private void FindAndMoveToTable()
    {
        assignedTable = customerManager.GetAvailableTable();

        if (assignedTable != null)
        {
            customerAgent.SetDestination(assignedTable.transform.position);
        }
        else
        {
            Debug.Log("No available tables for this customer.");
        }
    }

    private void CheckIfReachedTable()
    {
        if (assignedTable != null && !isSeated)
        {
            if (Vector3.Distance(transform.position, assignedTable.transform.position) < 2.0f)
            {
                isSeated = true;
                orderButton.gameObject.SetActive(true);
                hasOrdered = true;
                Debug.Log($"Order Accepted: {orderedDish}");
                customerManager.HandleOrder(this, orderedDish);
            }
        }
    }

    private void OnOrderButtonClick()
    {
        // if (hasOrdered) return;
        // hasOrdered = true;

        // Debug.Log($"Order Accepted: {orderedDish}");

        // customerManager.HandleOrder(this, orderedDish);
    }

    // private void CheckFoodOnTable()
    // {
    //     if (isEating || assignedTable.plateFood == null) return;

    //     Debug.Log("CheckFoodOnTable!");
    //     FoodData servedFood = FindFoodDataByType((Food)assignedTable.plateFood);

    //     if (servedFood == orderedDish)
    //     {
    //         Debug.Log("Correct dish received.");
    //         // customerManager.UpdateGameStats(servedFood.price, 10); // 평판 증가
    //         isFoodReceived = true;
    //     }
    //     else
    //     {
    //         Debug.Log("Wrong dish received.");
    //         //customerManager.UpdateGameStats(0, -5); // 평판 감소
    //     }

    //     // OrderManager에서 해당 주문 삭제
    //     customerManager.orderManager.RemoveOrder(orderedDish);

    //     StartEating();
    // }

    private void StartEating()
    {   
        if (isEating || assignedTable.plateFood == null) return;
        isEating = true;

        // 주문목록에서 삭제
        customerManager.orderManager.RemoveOrder(this, orderedDish);

        // 카운트다운 종료
        patienceTimer = 0;
        UpdatePatienceGauge();

        Debug.Log("StartEating");
        StartCoroutine(EatFood());
    }

    private IEnumerator EatFood()
    {
        // 먹는 사운드 재생
        if (audioSource != null && eatSound != null)
        {
            audioSource.clip = eatSound;
            audioSource.loop = true; // 필요 시 루프 설정
            audioSource.Play(); // 사운드 재생
        }

        // 음식이 맞게 왔는지 판단
        FoodData servedFood = FindFoodDataByType((Food)assignedTable.plateFood);
        if (servedFood == orderedDish)
        {
            Debug.Log("Correct dish received.");
            // customerManager.UpdateGameStats(servedFood.price, 10); // 평판 증가
            isFoodReceived = true;
        }
        else
        {
            Debug.Log("Wrong dish received.");
            //customerManager.UpdateGameStats(0, -5); // 평판 감소
        }

        // 음식이 맞게 왔다면 행복해 함
        if (isFoodReceived)
        {
            DisplayIcon(HappyIcon);
        }
        else
        {
            DisplayIcon(DisappointIcon);
        }
        
        yield return new WaitForSeconds(10f); // 음식을 먹는 시간

        audioSource.Stop(); // 오디오 종료;

        assignedTable.plateFood = null; // 테이블 비우기
        Destroy(assignedTable.currentPlateObject); // 프리팹 삭제

        

        ExitRestaurant();
    }

    public void ExitRestaurant()
    {
        if (assignedTable != null)
        {
            assignedTable.Vacate(); // 테이블 상태 비우기
        }

        customerAgent.SetDestination(spawnPosition);
        StartCoroutine(CheckIfReachedExit());
    }

    private IEnumerator CheckIfReachedExit()
    {
        while (Vector3.Distance(transform.position, spawnPosition) > 1.5f)
        {
            yield return null;
        }

        if (isFoodReceived)
        {
            int points = orderedDish.level * 10;
            Debug.Log($"You get {points}points");
            customerManager.UpdateGameStats(orderedDish.price, points);
        }

        customerManager.RemoveCustomer(this); // CustomerManager에서 삭제
        Destroy(gameObject); // 오브젝트 삭제
    }

    private void GetRandomDishFromCustomerManager()
    {
        orderedDish = customerManager.GetRandomDish();
        DisplayIcon(orderedDish.icon);
    }

    private void DisplayIcon(Texture Icon)
    {
        RawImage buttonRawImage = orderButton.transform.Find("Image").GetComponent<RawImage>();

        if (buttonRawImage != null)
        {
            buttonRawImage.texture = Icon;
            buttonRawImage.color = Color.white;
        }
    }

    /// <summary>
    /// 특정 Food 타입에 해당하는 FoodData를 검색합니다.
    /// </summary>
    private FoodData FindFoodDataByType(Food foodType)
    {
        foreach (FoodData foodData in foodDatabase.foodData)
        {   
            if (foodData.food == foodType)
            {
                return foodData;
            }
        }
        Debug.LogWarning($"Food type {foodType} not found in database.");
        return null;
    }
}
