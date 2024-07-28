using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    //�ϴ� �׽�Ʈ���̴ϱ� ü�� ����ġ ���� ���� �÷��̾ ���缭 �̺�Ʈ�� ����س����ø� �ɵ��մϴ�.
    
    public InventorySlot[] HotBarSlots;
    //public Inventory inventory; //�� �κ��� ���� �ָ��ѵ�, ���� �κ��丮�� �ִٰ� �������� �� Find�� ����Ҷ� ������ �ɼ� �־ �ӽ÷� �������� ���� �����Կ�

    [SerializeField] private InventoryItem[] hotitemSet;
    [SerializeField] private InventoryItem itemPrefab;


    private void Awake()
    {
        hotitemSet = new InventoryItem[HotBarSlots.Length];
    }

    private void Start()
    {
        //Inventory.instance.OnHotBarChanged += InitHotBar;
        Inventory.instance.OnHotBarChanged += SetHotBar;
        
    }

    private void SetHotBar()
    {
        for (int i = 0; i < HotBarSlots.Length; i++)
        {
            // �ֹ� ���Կ� �������� �ְ�, �κ��丮 ���Կ� �������� ���� ��� ����
            if (HotBarSlots[i] != null && Inventory.instance.inv_Slot[i] == null)
            {
                
                if (TryRemoveItem(HotBarSlots, hotitemSet, i, out InventoryItem removedItem))
                {
                    // �ʿ信 ���� ���ŵ� ���� �߰� �۾�, �Ƹ� ������ �����Ⱑ ���⼭ ���� ������?
                }
            }

            // �κ��丮 ���Կ� �������� �ִ� ��� ��ġ
            if (Inventory.instance.inv_Slot[i] != null)
            {
                var tempItem = Inventory.instance.inv_Slot[i];
                TryPlaceItem(HotBarSlots, hotitemSet, i, tempItem, itemPrefab);
            }
        }
    }

    public bool TryPlaceItem(InventorySlot[] slots, InventoryItem[] itemSet, int index, ItemComponent tempItem, InventoryItem itemPrefab)
    {
        if (slots[index].myItem == null)
        {
            if (itemSet.Length > index && itemSet[index] == null)
            {
                itemSet[index] = Instantiate(itemPrefab, slots[index].transform);
                itemSet[index].Initialize(tempItem, slots[index]);
                return true; // �������� ��ġ�����Ƿ� true ��ȯ
            }
        }
        return false; // ������ ��ġ ����
    }

    public bool TryRemoveItem(InventorySlot[] slots, InventoryItem[] itemSet, int index, out InventoryItem removedItem)
    {
        // out �Ű������� �ʱ�ȭ�մϴ�
        removedItem = null;

        // ������ ���Կ� �������� �ִ��� Ȯ���մϴ�
        if (slots[index].myItem != null)
        {
            //Debug.Log("removesetSlots : " + index);

            // itemSet���� ������ �ε����� �������� �ִ��� Ȯ���մϴ�
            if (itemSet.Length > index && itemSet[index] != null)
            {
                // itemSet���� �������� �����ɴϴ�
                removedItem = itemSet[index];

                // itemSet�� ���Կ��� �������� �����մϴ�
                itemSet[index] = null;
                slots[index].myItem = null;

                // �ʿ信 ���� �������� GameObject�� �ı��մϴ�
                Destroy(removedItem.gameObject);

                return true; // �������� ���������� ���ŵ��� ��ȯ
            }

            return true;
        }

        //Debug.Log("bugbugbug");
        return false; // ������ ���� ���и� ��ȯ
    }



}