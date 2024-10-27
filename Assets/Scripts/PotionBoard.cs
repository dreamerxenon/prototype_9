using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;



public class PotionBoard : MonoBehaviour
{
  public int width;
  public int height;

  public float spacingY;
  public float spacingX;
  public GameObject[] potionPrefabs;
  private Node[,] potionBoard;
  public GameObject[] potionBoardGo;
  public List<GameObject> potionsToDestroy;
   public GameObject potionParent;
  [SerializeField]
  private Potion SelectedPotion;
   [SerializeField]
  private bool isProcessingMove;
   [SerializeField]
   List<Potion> potionsToRemove = new();
    public AudioClip[] portionEffects;
    public AudioClip positiveSwitch;
    public AudioClip negativeSwitch;
    private AudioSource audioSource;
    public bool booster, boosterbought;

    public ArrayLayout arrayLayout;

  public static PotionBoard Instance;

  void Awake()
  {
    Instance = this;
  }

    // Update is called once per frame
    void Start()
    {
        InitializeBoard(false);
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject.GetComponent<Potion>())
            {
                if (isProcessingMove)
                    return;

                Potion potion = hit.collider.gameObject.GetComponent<Potion>();
                Debug.Log("I have clicked" + potion.gameObject);
                SelectPotions(potion);
            }
        }
    }

    public void InitializeBoard(bool _specialBoosterBought)
    {
        DestroyPotions();
          potionBoard =  new Node[width, height];
          spacingX = (float)(width - 1) / 2;
          spacingY = (float)((height - 1) / 2) + 0.5f;

          for(int y = 0; y < height; y++)
          {
             for (int x = 0; x < width; x++)
             {
              Vector2 position = new Vector2(x - spacingX, y - spacingY);
              if(arrayLayout.rows[y].row[x])
              {
                potionBoard[x, y] = new Node(false, null);
              }else
              {
                if(_specialBoosterBought)
                    {
                        int randomIndex = Random.Range(0, potionPrefabs.Length);
                        GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                        potion.transform.SetParent(potionParent.transform);
                        potion.GetComponent<Potion>().SetIndicies(x, y);
                        potionBoard[x, y] = new Node(true, potion);
                        potionsToDestroy.Add(potion);
                        boosterbought = true;
                        
                    }
                    else
                    {
                        int randomIndex = Random.Range(0,potionPrefabs.Length - 1);
                        GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                        potion.transform.SetParent(potionParent.transform);
                        potion.GetComponent<Potion>().SetIndicies(x, y);
                        potionBoard[x, y] = new Node(true, potion);
                        potionsToDestroy.Add(potion);
                        boosterbought = false;
                    }
               
                
              }
             }
          }

          if(CheckBoard())
        {
            Debug.Log("we have matches restart the game");
            if(_specialBoosterBought)
            {
                InitializeBoard(true);
            }
            else
            {
                InitializeBoard(false);
            }
            
        }
        else
        {
            Debug.Log("No matches");
        }
    }
    public bool CheckBoard()
    {

        if (GameManager.Instance.isGameEnded)
            return false;

      bool hasMatched =  false;
        potionsToRemove.Clear();
      
        foreach(Node nodePotion in  potionBoard)
        {
            if(nodePotion.potion !=  null)
            {
                nodePotion.potion.GetComponent<Potion>().isMatched = false;
            }
        }

        for (int y = 0; y < height;y++)
        {
            for  (int x = 0;  x < width;x++)
            {
                if (potionBoard[x, y].isUsable)
                {
                    Potion potion = potionBoard[x, y].potion.GetComponent<Potion>();

                    if(!potion.isMatched)
                    {
                        MatchResult matchedPotions = IsConnected(potion);
                        if (matchedPotions.connectedPotions.Count >= 3)
                        {
                            MatchResult superMatchedPotions = SuperMatch(matchedPotions);
                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);
                            foreach (Potion pot in superMatchedPotions.connectedPotions)
                            {
                                pot.isMatched = true;
                                hasMatched = true;
                            }
                        }
                    }
                }
            }
        }
       

        return hasMatched;
    }

    private IEnumerator ProcessTurnOnMatchBoard(bool _subtractMoves)
    {
        foreach (Potion potionToRemove in potionsToRemove)
        {
            potionToRemove.isMatched = false;
        }
        RemoveAndRefill(potionsToRemove);
        foreach (Potion potionToRemove in potionsToRemove)
        {
            if(potionToRemove.potionType == PotionType.Booster)
            {
                booster = true;
            }
        }
        if (booster)
        {
            GameManager.Instance.ProcessTurn(15, _subtractMoves);
        }
        else
        {
            GameManager.Instance.ProcessTurn(potionsToRemove.Count, _subtractMoves);
        }
           

        
        yield return new WaitForSeconds(.4f);

        if(CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchBoard(false));
        }
    }

    private void RemoveAndRefill(List<Potion> _potionsToRemove)
    {
        foreach(Potion potion in _potionsToRemove)
        {
            int xIndex = potion.xIndex;
            int yIndex = potion.yIndex;
            int randomIndex = Random.Range(0, 2);
            audioSource.clip = portionEffects[randomIndex];
            Destroy(potion.gameObject);
            audioSource.Play();


            potionBoard[xIndex, yIndex] = new Node(true, null);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (potionBoard[x,y].potion == null)
                    {
                        RefillPotion(x, y);
                    }
                    
                }
            }
        }
    }
   
    private void RefillPotion(int x, int y)
    {
        int yOffset = 1;

        while (y + yOffset < height && potionBoard[x, y + yOffset].potion == null)
        {
            yOffset++;
        }
        if(y + yOffset < height && potionBoard[x, y + yOffset].potion != null)
        {
            Potion potionAbove = potionBoard[x, y + yOffset].potion.GetComponent<Potion>();

            Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, potionAbove.transform.position.z);

            potionAbove.MoveToTarget(targetPos);

            potionAbove.SetIndicies(x, y);

            potionBoard[x, y] = potionBoard[x, y + yOffset];

            potionBoard[x, y + yOffset] = new Node(true, null);
        }

        if ( y + yOffset ==  height)
        {
            SpawnPotionAtTop(x);
        }


    }
    #region Cascading Potions
    private void SpawnPotionAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);
        int locationToMoveTo = height - index;
        if(boosterbought)
        {
            int randomIndex = Random.Range(0, potionPrefabs.Length);
            GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
            newPotion.transform.SetParent(potionParent.transform);

            newPotion.GetComponent<Potion>().SetIndicies(x, index);

            potionBoard[x, index] = new Node(true, newPotion);
            Vector3 targetPosition = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMoveTo, newPotion.transform.position.z);
            newPotion.GetComponent<Potion>().MoveToTarget(targetPosition);
        }
        else
        {
            int randomIndex = Random.Range(0, potionPrefabs.Length - 1);
            GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
            newPotion.transform.SetParent(potionParent.transform);

            newPotion.GetComponent<Potion>().SetIndicies(x, index);

            potionBoard[x, index] = new Node(true, newPotion);
            Vector3 targetPosition = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMoveTo, newPotion.transform.position.z);
            newPotion.GetComponent<Potion>().MoveToTarget(targetPosition);
        }
       

    }

    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = 99;
        for (int y = height - 1; y >= 0; y--)
        {
            if (potionBoard[x, y].potion == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }

    #endregion
    #region matching logic
    MatchResult IsConnected(Potion potion)
    {
        List<Potion> connectedPotions = new();
        PotionType potionType = potion.potionType; 
        connectedPotions.Add(potion);

        //check right
        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);
        //check left
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);

        //check for 3 match horizontal
        if(connectedPotions.Count == 3)
        {
            Debug.Log(connectedPotions[0].potionType + " is horizontally connected");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.horizontal
            };
        } //check for long horizontal
        else if (connectedPotions.Count > 3)
        {
            Debug.Log(connectedPotions[0].potionType + " is longhorizontally connected");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.longHorizontal
            };
        }

        //clear connectedPotions
        connectedPotions.Clear();
        //add initialPotion
        connectedPotions.Add(potion);

        //check up
        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);
        //check down
        CheckDirection(potion, new Vector2Int(0, -1), connectedPotions);



        //check for 3 match vertical
        if (connectedPotions.Count == 3)
        {
            Debug.Log(connectedPotions[0].potionType + " is vertically connected");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.vertical
            };
        }//check for long vertical
        else if (connectedPotions.Count > 3)
        {
            Debug.Log(connectedPotions[0].potionType + " is longvertically connected");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.longVertical
            };
        }
        else
        {
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction =  MatchDirection.none
            };
        }
       
    }

    void CheckDirection(Potion pot, Vector2Int direction ,List<Potion> connectedPotions)
    {
        PotionType potionType = pot.potionType; 
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        while(x >= 0 && x < width && y >= 0 && y < height)
        {
            if (potionBoard[x, y].isUsable)
            {
                Potion neighbourPotion = potionBoard[x, y].potion.GetComponent<Potion>();

                if (!neighbourPotion.isMatched && neighbourPotion.potionType == potionType)
                {

                    connectedPotions.Add(neighbourPotion);

                    x += direction.x;
                    y += direction.y;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    private void DestroyPotions()
    {
        if(potionsToDestroy != null)
        {
            foreach (GameObject go in potionsToDestroy)
            {
                Destroy(go);
            }
            potionsToDestroy.Clear();
        }
    }
#endregion
    #region Swapping Potions
    //select potion
    public void SelectPotions(Potion _potion)
    {
        if (SelectedPotion == null)
        {
            Debug.Log(_potion);
            SelectedPotion = _potion;
        } else if (SelectedPotion == _potion)
        {
            SelectedPotion = null;
            

        } else if (SelectedPotion != _potion)
        {
            SwapPotion(SelectedPotion, _potion);
            SelectedPotion = null;
        }
    }
    //swap logic
    public void SwapPotion(Potion _currentPotion, Potion _targetPotion)
    {
        if (!IsAdjacent(_currentPotion, _targetPotion))
        {
            audioSource.clip = negativeSwitch;
            audioSource.Play();
            return;

        } else
        {
            //DoSwap
            DoSwap(_currentPotion, _targetPotion);
            audioSource.clip = positiveSwitch;
            audioSource.Play();
        }

        isProcessingMove = true;
        StartCoroutine(ProcessesMatches(_currentPotion,_targetPotion));

    }
     //DoSwap
     private void DoSwap(Potion _currentPotion, Potion _targetPotion)
    {
        GameObject temp = potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion;
        potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion = potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion;
        potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion = temp;

        int tempXindex = _currentPotion.xIndex;
        int tempYindex = _currentPotion.yIndex;
        _currentPotion.xIndex = _targetPotion.xIndex;
        _currentPotion.yIndex = _targetPotion.yIndex;
        _targetPotion.xIndex = tempXindex;
        _targetPotion.yIndex = tempYindex;

        _currentPotion.MoveToTarget(potionBoard[_targetPotion.xIndex,_targetPotion.yIndex].potion.transform.position);
        _targetPotion.MoveToTarget(potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion.transform.position);
    }

    private IEnumerator ProcessesMatches(Potion _currentPotion, Potion _targetPotion)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchBoard(true));
            isProcessingMove = false;
        }
        else
        {
           
            
                DoSwap(_currentPotion, _targetPotion);
            GameManager.Instance.ProcessTurn(0, true);
            
            isProcessingMove = false;
        }

    }


    //isAdjacent
    private bool IsAdjacent(Potion _currentPotion, Potion _targetPotion)
    {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }

    #endregion

    private MatchResult SuperMatch(MatchResult _matchResults)
    {
       //check for horizontal & long horizontal
       if(_matchResults.direction == MatchDirection.horizontal || _matchResults.direction == MatchDirection.longHorizontal)
        {
            foreach (Potion pot in _matchResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();
                 CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPotions);

                if(extraConnectedPotions.Count >=2)
                {
                    Debug.Log("i have a super horizontal match");
                    extraConnectedPotions.AddRange(_matchResults.connectedPotions);

                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.super
                    };

                }
            }
            return new MatchResult
            {
                connectedPotions = _matchResults.connectedPotions,
                direction = _matchResults.direction


            };
        }

        //check for vertical & longvertical
        if (_matchResults.direction == MatchDirection.vertical || _matchResults.direction == MatchDirection.longVertical)
        {
            foreach (Potion pot in _matchResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();
                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPotions);

                if (extraConnectedPotions.Count >= 2)
                {
                    Debug.Log("i have a super vertical match");
                    extraConnectedPotions.AddRange(_matchResults.connectedPotions);

                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.super
                    };

                }
            }
            return new MatchResult
            {
                connectedPotions = _matchResults.connectedPotions,
                direction = _matchResults.direction


            };
        }
        return null;
    }
}

public class MatchResult
{
     public List<Potion> connectedPotions;
    public MatchDirection direction;
}

public enum MatchDirection
{
    vertical,
    horizontal,
    longVertical,
    longHorizontal,
    super,
    none
}
