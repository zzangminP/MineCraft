using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    
    public InventoryItem carriedItem;
   

    [SerializeField] public InventorySlot[] inventorySlots; //1�� ���� �迭
    [SerializeField] public InventorySlot[] hotbarSlots; //2�� ���� �迭

    // 0: HEAD, 1: CHEST, 2: LEGGINGS, 3: FEET, 4: WEAPON, 5: Accessories
    [SerializeField] public InventorySlot[] equipmentSlots; //�ۺ��� ������ �ʹ� ���Ŵϴ�

    [SerializeField] private Transform draggablesTransform;
    [SerializeField] private InventoryItem itemPrefab;

    [Header("Item List")]
    
    [SerializeField] private List<Original_Item> item_list;
    [SerializeField] private InventoryItem[] itemSet;
    [SerializeField] private InventoryItem[] hotitemSet;

    private UIManager UIManager;

    //[SerializeField] private Button giveItemBtn;

    void Awake()
    {
        
        itemSet = new InventoryItem[inventorySlots.Length];
        hotitemSet = new InventoryItem[hotbarSlots.Length];

        UIManager = GetComponentInParent<UIManager>();
        if(UIManager == null)
        {
            Debug.LogError("UI����");
        }


        //SetEmptyItem();
    }

    private void OnEnable()
    {
        SetInventory();
    }


    private void Start()
    {
        
        Add_All_Item();

      
    }
    private void Add_All_Item()
    {
        foreach (int key in Item_Dictionary.item_dictionary.Keys)
        {
            item_list.Add(Item_Dictionary.item_dictionary[key]);
        }
    }

    void Update()
    {
        if (carriedItem == null) return;

        carriedItem.transform.position = Input.mousePosition;
    }

    public void SetCarriedItem(InventoryItem item)
    {
        if (carriedItem != null)
        {
            if (item.activeSlot != null && item.activeSlot.myItem != null && item.activeSlot.myItem.equip_type != Equipment_Type.NONE && item.activeSlot.myItem.equip_type != carriedItem.equip_type) return;
            item.activeSlot.SetItem(carriedItem);
        }

        if (item != null &&
            item.activeSlot != null &&
            item.activeSlot.myItem != null &&
            item.activeSlot.myItem.equip_type != Equipment_Type.NONE)
        {
            EquipEquipment(item.activeSlot.Equip_Type, null);
        }
        else
        {
            // ��� �κп��� null�� �߻��ߴ��� ����� �α� �߰�
            if (item == null)
            {
                Debug.LogError("Item is null.");
            }
            else if (item.activeSlot == null)
            {
                Debug.LogError("item.activeSlot is null.");
            }
            else if (item.activeSlot.myItem == null)
            {
                Debug.LogError("item.activeSlot.myItem is null.");
            }
            else if (item.activeSlot.myItem.equip_type == Equipment_Type.NONE)
            {
                Debug.LogError("item.activeSlot.myItem.equip_type is NONE.");
            }
        }

        carriedItem = item;
        carriedItem.canvasGroup.blocksRaycasts = false;
        item.transform.SetParent(draggablesTransform);
    }

    public void EquipEquipment(Equipment_Type tag, InventoryItem item = null)
    {
        switch (tag)
        {
            case Equipment_Type.HELMET:
                if (item == null)
                {
                    //���߿� �Ұž� �̰�
                }
                else
                {
                    //Debug.Log("Equipped " + item.myItem.name + " on " + tag);
                }
                break;
            case Equipment_Type.CHESTPLATE:
                break;
            case Equipment_Type.LEGGINGS:
                break;
            case Equipment_Type.BOOTS:
                break;
            case Equipment_Type.ONE_HANDED_SWORD: //�̰� �� �� �پ缺 �߰��� ����
                break;
            case Equipment_Type.SHIELD: //��ű� ĭ�ε� ��� �ɵ�?
                break;
        }
    }

    public void SpawnCollidedItem(ItemComponent item)
    {
        if (item == null)
        {
            Debug.LogError("ItemComponent is null");
            return;
        }

        for (int i = 0; i < Inventory.instance.inv_Slot.Length; i++)
        {
            if(Inventory.instance.inv_Slot[i] != null)
            {
                var tempItem = Inventory.instance.inv_Slot[i];
                

                if (i < hotbarSlots.Length)
                {
                    if (UIManager.TryPlaceItem(hotbarSlots, hotitemSet, i, tempItem, itemPrefab))
                        return;
                }
                else
                {
                    if (UIManager.TryPlaceItem(inventorySlots, itemSet, i - hotbarSlots.Length, tempItem, itemPrefab)) //�� �κ� �ſ� �߿���, �κ��丮�� �� �κ��� �ֹٷ� ó���ϱ� ���� ����
                        return;
                }
            }

        }

    }

    public void SetInventory()
    {
        //inventory.instance.GetInv_Main()[0].

        ItemComponent[] inv = Inventory.instance.inv_Slot;
        
        int invLength = Inventory.instance.inv_Slot.Length;
        
        for (int i = 0; i < invLength; i++)
        {
            if(inv[i] != null)
            {
                //Debug.Log("invindex : " + i);
                SpawnCollidedItem(inv[i]); 
            }

        }

    }

}