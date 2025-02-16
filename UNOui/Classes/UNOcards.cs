using Project.Assets.ControlClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using UNOui.UserControls;

namespace UNOui
{
    public class Player : CardHolder
    {
        public bool IsDrawing = false;

        public Player(string name)
        {
            this.Name = name;
        }

        public void CheckForUno()
        {
            if (Cards.Count == 2)
            {
                IsUno = true;
            }
            if (IsUno == true && Cards.Count == 1)
            {
                StartUNOTimer();
            }
        }
        public void PlayCard()
        {
            Canvas.SetZIndex(Table.draggedimage, Items.GameItem.CardZIndex);
            double bottom = Canvas.GetTop(Table.draggedimage);
            if (bottom < Items.GameItem.gamecanvas.ActualHeight * (Table.draggedimage.Height / (Items.GameItem.gamecanvas.ActualHeight / 2)) && Card.ImageToCard(Table.draggedimage).CardIsPlayable())
            {
                Card draggedcard = Card.ImageToCard(Table.draggedimage);
                Random random = new Random();
                int randomnumber = random.Next(-45, 45);
                if (Cards.Count > 1)
                {
                    Table.AddToTopCards(randomnumber);
                }
                draggedcard.PlayCard();
                Table.topcard.image.RenderTransform = Card.rotate(randomnumber);
                Table.topcard.SetZIndexToOne();
                Cards.Remove(draggedcard);
                CheckForUno();
                Items.GameItem.gamecanvas.Children.Remove(draggedcard.image);
                Canvas.SetZIndex(Table.draggedimage, Items.GameItem.CardZIndex);
                if (Table.topcard.number != -1)
                {
                    Table.SetNextTurn();
                }
                Task.Delay(TimeSpan.FromSeconds(0.5)).ContinueWith(task =>
                {
                    if ((draggedcard.number == -4 || draggedcard.number == -5) && draggedcard.color == "none")
                    {
                        UserControl changecolor = new ColorChange();
                        Items.GameItem.gamegrid.Children.Add(changecolor);
                    }
                    else
                    {
                        Items.GameItem.player.CheckForWildCards();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
        public void StartUNOTimer()
        {
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(task =>
            {
                if (IsUno == true)
                {
                    Cards.Add(Card.GetRandomCard());
                    Task.Delay(TimeSpan.FromSeconds(0.5)).ContinueWith(task =>
                    {
                        Cards.Add(Card.GetRandomCard());
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        public override void RefreshCards(int gappy)
        {
            Items.GameItem.gamecanvas.Children.Clear();
            Table.topcard.image.Width = Table.topcard.image.Height = 150;
            Canvas.SetTop(Table.topcard.image, Items.GameItem.gamecanvas.ActualHeight / 3);
            Canvas.SetLeft(Table.topcard.image, Items.GameItem.gamecanvas.ActualWidth / 2 - Table.topcard.image.Width / 2);
            Items.GameItem.gamecanvas.Children.Add(Table.topcard.image);
            int gap = ((Items.GameItem.player.Cards.Count * 150 - Items.GameItem.player.Cards.Count * 100) / 2) - 30;
            for (int index = 0; index < Items.GameItem.player.Cards.Count; index++)
            {
                Image image = Items.GameItem.player.Cards[index].image;
                image.Width = 150;
                image.Height = 150;
                Canvas.SetTop(image, (int)Items.GameItem.gamecanvas.ActualHeight - image.Width);
                Canvas.SetLeft(image, (int)Items.GameItem.gamecanvas.ActualWidth / 2 - (image.Width / 2) + index * 50 - gap);
                Items.GameItem.gamecanvas.Children.Add(image);
            }
            RefreshUNO();
        }

        public void UNOPressed(object sender, MouseButtonEventArgs e)
        {
            Audio.PlayUNOSound();
            Items.GameItem.gamecanvas.Children.Remove((Image)sender);
            IsUno = false;
        }

        public void RefreshUNO()
        {
            if (Cards.Count > 2 || !IsUno || (Table.turn != 1 && Cards.Count != 1))
            {
                return;
            }
            if (Cards.Count == 2 && !CanPlay())
            {
                return;
            }
            Image unobutton = new Image();
            unobutton.Width = unobutton.Height = 150;
            unobutton.Source = Card.CardNameToImage("unobutton").Source;
            unobutton.MouseDown += UNOPressed;
            Canvas.SetBottom(unobutton, 10);
            Canvas.SetRight(unobutton, 10);
            Items.GameItem.gamecanvas.Children.Add(unobutton);
        }

        public void DeckDown(object sender, MouseButtonEventArgs e)
        {
            if ((e != null && IsDrawing) || Table.turn != 1) { return; }
            IsDrawing = true;
            AddCard();
            Audio.PlayCardTakeSound();
            Card card = Cards[Cards.Count - 1];
            if (card.CardIsPlayable())
            {
                IsDrawing = false;
                UserControl draworplay = new DrawOrPlay();
                Items.GameItem.gamegrid.Children.Add(draworplay);
            }
            else
            {
                if (Settings.DrawUntilPlayable == 1 && !card.CardIsPlayable())
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(task =>
                    {
                        DeckDown(new object(), null);
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    IsDrawing = false;
                    Table.SetNextTurn();
                    Table.CheckForTurn();
                }
            }
        }
    }
    public class Bot : CardHolder
    {
        public int Position;
        public bool IsItsTurn;

        public Bot(int Position, string Name)
        {
            this.Position = Position;
            this.Name = Name;
        }

        DropShadowEffect dropShadowEffect = new DropShadowEffect()
        {
            Color = Colors.Blue,
            Direction = 135,
            ShadowDepth = 5,
            Opacity = 0.7,
        };

        public void StartUnoTimer()
        {
            RefreshUNO();
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(task =>
            {
                if(IsUno == true)
                {
                    Audio.PlayUNOSound();
                }
                IsUno = false;
                Table.RefreshVisuals();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        public void CheckForUno()
        {
            if (Cards.Count == 1)
            {
                if(Items.MainWindowItem.RandomInteger(0, 2) == 0)
                {
                    Audio.PlayUNOSound();
                }
                else
                {
                    IsUno = true;
                    StartUnoTimer();
                }
            }
        }
        public void CardChangeColor(out string color, out string imagepath, int number)
        {
            int blue = 0;
            int yellow = 0;
            int green = 0;
            int red = 0;
            for(int index = 0; index < Cards.Count; index++)
            {
                switch(Cards[index].color)
                {
                    case "Blue":
                        blue++;
                        break;
                    case "Yellow":
                        yellow++;
                        break;
                    case "Green":
                        green++;
                        break;
                    case "Red":
                        red++;
                        break;
                }
            }

            int max = Math.Max(blue, Math.Max(yellow, Math.Max(green, red)));
            color = "";
            imagepath = "";
            if(max == blue)
            {
                color = "Blue";
                imagepath = (number == -5 ? "bluewildcard" : "bluedrawfour");
            }
            else if (max == yellow)
            {
                color = "Yellow";
                imagepath = (number == -5 ? "yellowwildcard" : "yellowdrawfour");
            }
            else if (max == green)
            {
                color = "Green";
                imagepath = (number == -5 ? "greenwildcard" : "greendrawfour");
            }
            else if (max == red)
            {
                color = "Red";
                imagepath = (number == -5 ? "redwildcard" : "reddrawfour");
            }
        }

        public Card? PlayCardLogic()
        {
            bool found = false;
            Card card = new Card
            {
                number = -6,
            };

            for (int index = 0; index < Cards.Count; index++)
            {
                if (Cards[index].number > card.number && Cards[index].CardIsPlayable())
                {
                    found = true;
                    card = Cards[index];
                }
            }

            if (found)
            {
                return card;
            }

            return null;
        }

        public void UNOPressed(object sender, MouseButtonEventArgs e)
        {
            if (IsUno == false)
            {
                return;
            }
            Items.GameItem.gamecanvas.Children.Remove((Image)sender);
            Cards.Add(Card.GetRandomCard());
            Task.Delay(TimeSpan.FromSeconds(0.5)).ContinueWith(task =>
            {
                Cards.Add(Card.GetRandomCard());
                IsUno = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void RefreshUNO()
        {
            if (Cards.Count != 1)
            {
                return;
            }
            if(IsUno == false)
            {
                return;
            }
            Image unobutton = new Image();
            unobutton.Width = unobutton.Height = 150;
            unobutton.Source = Card.CardNameToImage("unobutton").Source;
            unobutton.MouseDown += UNOPressed;
            Canvas.SetBottom(unobutton, 10);
            Canvas.SetRight(unobutton, 10);
            Items.GameItem.gamecanvas.Children.Add(unobutton);
        }

        public void PlayCard()
        {
            if (Cards.Count == 0 || Table.HasSomeoneWon()) { return; }
            IsItsTurn = true;
            Table.RefreshVisuals();
            int randomnumber = Items.MainWindowItem.RandomInteger(-45, 45);
            double delay = Items.MainWindowItem.RandomDouble(1, 2);
            Task.Delay(TimeSpan.FromSeconds(delay)).ContinueWith(task =>
            {
                Card? logiccard = PlayCardLogic();
                if (logiccard != null)
                {
                    Table.AddToTopCards(randomnumber);
                    logiccard.PlayCard();
                    Card card = logiccard;
                    Cards.Remove(card);
                    CheckForUno();
                    if (Table.topcard.number != -1)
                    {
                        Table.SetNextTurn();
                    }
                    CheckForWildCards();
                    Table.topcard.SetZIndexToOne();
                    Table.topcard.image.RenderTransform = Card.rotate(randomnumber);
                    IsItsTurn = false;
                    Table.RefreshVisuals();   
                }
                else
                {
                    Card newcard = Card.GetRandomCard();
                    Audio.PlayCardTakeSound();
                    Cards.Add(newcard);
                    Table.RefreshVisuals();
                    if (newcard.CardIsPlayable())
                    {
                        Task.Delay(TimeSpan.FromSeconds(1.5)).ContinueWith(task => {
                            Table.AddToTopCards(randomnumber);
                            newcard.PlayCard();
                            Card card = newcard;
                            Cards.Remove(newcard);
                            CheckForUno();
                            if (Table.topcard.number != -1)
                            {
                                Table.SetNextTurn();
                            }
                            CheckForWildCards();
                            Table.topcard.SetZIndexToOne();
                            Table.topcard.image.RenderTransform = Card.rotate(randomnumber);
                            IsItsTurn = false;
                            Table.RefreshVisuals();
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else
                    {
                        if(Settings.DrawUntilPlayable == 1)
                        {
                            PlayCard();
                        }
                        else
                        {
                            Table.SetNextTurn();
                            Table.CheckForTurn();
                            IsItsTurn = false;
                            Table.RefreshVisuals();
                        }
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        override public void RefreshCards(int gap)
        {
            if (Settings.PlayerCount >= 2)
            {
                int initialgap = 4;
                for (int index = 0; index < Cards.Count; index++)
                {
                    Image image = new Image();
                    if (IsItsTurn)
                    { 
                        image.Effect = dropShadowEffect;
                    }
                    RotateTransform rotate = new RotateTransform(gap - (Cards.Count * initialgap) / 4);
                    image.Width = image.Height = 150;
                    rotate.CenterX = image.Width * 0.73;
                    rotate.CenterY = image.Height * 0.89;
                    image.Source = Card.CardNameToImage("cardbackground").Source;
                    image.RenderTransform = rotate;
                    switch (Position)
                    {
                        case 1:
                            Canvas.SetLeft(image, 20);
                            Canvas.SetTop(image, Items.GameItem.gamecanvas.ActualHeight / 3);
                            break;
                        case 2:
                            Canvas.SetLeft(image, Items.GameItem.gamecanvas.ActualWidth / 2 - (image.Width / 2));
                            Canvas.SetTop(image, 20);
                        break;
                        case 3:
                            Canvas.SetLeft(image, Items.GameItem.gamecanvas.ActualWidth - 200);
                            Canvas.SetTop(image, Items.GameItem.gamecanvas.ActualHeight / 3);
                            break;
                    }
                    TextBlock nickname = new TextBlock()
                    {
                        Text = (Settings.Language == 2 ? "Бот " : "Bot ") + Name,
                        FontSize = 50,
                        FontWeight = FontWeights.Bold,
                        Foreground = IsItsTurn ? Brushes.Blue : Brushes.Black,
                    };
                    Canvas.SetLeft(nickname, Canvas.GetLeft(image) - image.ActualWidth / 2 + 20);
                    Canvas.SetTop(nickname, Canvas.GetTop(image) + 140);
                    Items.GameItem.gamecanvas.Children.Add(nickname);
                    Items.GameItem.gamecanvas.Children.Add(image);
                    gap = gap + initialgap;
                    RefreshUNO();
                }
            }
        }
    }

    public abstract class CardHolder
    {
        public abstract void RefreshCards(int gap);

        public bool IsUno = false;
        public string? Name;
        public int Points;
        public List<Card> Cards = new List<Card>();
        public static List<CardHolder> AllCards = new List<CardHolder>();

        public bool CanPlay()
        {
            for (int index = 0; index < Cards.Count; index++)
            {
                if (Cards[index].CardIsPlayable())
                {
                    return true;
                }
            }
            return false;
        }
        public class CardConstants
        {
            public const int WILD_CARD = -5;
            public const int DRAW_FOUR = -4;
            public const int REVERSE = -1;
            public const int DRAW_TWO = -2;
            public const int BLOCK = -3;
        }

        public void CheckForWildCards()
        {
            if (Table.topcard.number == CardConstants.WILD_CARD)
            {
                string color;
                string imagepath;
                ((Bot)this).CardChangeColor(out color, out imagepath, CardConstants.WILD_CARD);
                Table.topcard.color = color;
                Table.topcard.image = Card.CardNameToImage(imagepath);
                Table.RefreshVisuals();
                Card.SetTurneDelay();
            }
            else if (Table.topcard.number == CardConstants.DRAW_FOUR)
            {
                string color;
                string imagepath;
                ((Bot)this).CardChangeColor(out color, out imagepath, CardConstants.DRAW_FOUR);
                Table.topcard.color = color;
                Table.topcard.image = Card.CardNameToImage(imagepath);
                Table.RefreshVisuals();
                Task.Delay(TimeSpan.FromSeconds(0.5)).ContinueWith(task =>
                {
                    CardHolder.AllCards[Table.turn - 1].AddCard();
                }, TaskScheduler.FromCurrentSynchronizationContext());
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(task =>
                {
                    CardHolder.AllCards[Table.turn - 1].AddCard();
                }, TaskScheduler.FromCurrentSynchronizationContext());
                Task.Delay(TimeSpan.FromSeconds(1.5)).ContinueWith(task =>
                {
                    CardHolder.AllCards[Table.turn - 1].AddCard();
                }, TaskScheduler.FromCurrentSynchronizationContext());
                Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(task =>
                {
                    CardHolder.AllCards[Table.turn - 1].AddCard();
                    Table.SetNextTurn();
                    Table.CheckForTurn();
                    Table.RefreshVisuals();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (Table.topcard.number == CardConstants.REVERSE)
            {
                Table.SetNextTurn();
                Card.SetTurneDelay();
            }
            else if (Table.topcard.number == CardConstants.DRAW_TWO)
            {
                CardHolder.AllCards[Table.turn - 1].AddCard();

                Task.Delay(TimeSpan.FromSeconds(0.5)).ContinueWith(task =>
                {
                    CardHolder.AllCards[Table.turn - 1].AddCard();
                    Table.SetNextTurn();
                    Card.SetTurneDelay();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (Table.topcard.number == CardConstants.BLOCK)
            {
                if (Settings.PlayerCount == 2)
                {
                    Table.SetNextTurn();
                    Table.SetNextTurn();
                    Table.CheckForTurn();
                    return;
                }
                Table.direction = !Table.direction;
                Table.SetNextTurn();
                Table.CheckForTurn();
            }
            else
            {
                Table.CheckForTurn();
            }
        }


        public void RemoveCard(Card card)
        {
            Items.GameItem.gamecanvas.Children.Remove(card.image);
            Cards.Remove(card);
            Table.RefreshVisuals();
        }

        public void AddCard()
        {
            Card card = Card.GetRandomCard();
            card.image.MouseEnter += card.CardMouseEnter;
            card.image.MouseLeave += card.CardMouseLeave;
            card.image.PreviewMouseDown += Items.GameItem.CardMouseDown;
            Items.GameItem.PreviewMouseMove += Items.GameItem.CardMouseMove;
            Items.GameItem.gamecanvas.PreviewMouseUp += Items.GameItem.CardMouseUp;
            card.image.MouseDown += Items.GameItem.CardMouseDown;
            Cards.Add(card);
            Table.RefreshVisuals();
        }

        public void SetCards()
        {
            Cards.Clear();

            for(int index = 0; index < Settings.CardCount; index++)
            {
                AddCard();
            }
        }
    }

    public class Card
    {
        public Image image;
        public int number;
        public string color;

        public Card() {  }

        public Card(Image image, int number, string color)
        {
            this.image = image;
            this.number = number;
            this.color = color;
        }

        public Card(Card card)
        {
            image = card.image;
            number = card.number;
            color = card.color;
        }
        
        public void SetZIndexToOne()
        {
            Canvas.SetZIndex(image, 1);
        }

        public bool IsWildCard()
        {
            return number == -4 || number == -5;
        }

        public void PlayCard()
        {
            Audio.PlayCardSound();
            Table.topcard.image.Source = image.Source;
            Table.topcard.number = number;
            Table.topcard.color = color;
        }

        public void CardMouseEnter(object sender, MouseEventArgs e)
        {
            if(Table.turn != 1){ return; }
            Canvas.SetTop(image, (int)Items.GameItem.gamecanvas.ActualHeight - image.Width - 30);
        }

        public void CardMouseLeave(object sender, MouseEventArgs e)
        {
            Canvas.SetTop(image, (int)Items.GameItem.gamecanvas.ActualHeight - image.Width);
            Table.RefreshVisuals();
        }

        public bool CardIsPlayable()
        {
            return color == Table.topcard.color || number == Table.topcard.number || ((number == -4 || number == -5) && color == "none");
        }

        public static Image CardNameToImage(string card)
        {
            Image image = new Image();
            string source = Items.UNOCardsFilePath + card + ".png";
            image.Source = new BitmapImage(new Uri(source, UriKind.Relative));
            return image;
        }

        public static void SetTurneDelay()
        {
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(task =>
            {
                Table.CheckForTurn();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Card GetRandomCard()
        { 
            List<Card> cards = new List<Card>
            {
                //Draw cards
                new Card(CardNameToImage("drawfour"), -4, "none"),
                new Card(CardNameToImage("wildcard"), -5, "none"),

                //Yellow
                new Card(CardNameToImage("yellowreverse"), -1, "Yellow"),
                new Card(CardNameToImage("yellowdrawtwo"), -2, "Yellow"),
                new Card(CardNameToImage("yellowblock"), -3, "Yellow"),
                new Card(CardNameToImage("yellowone"), 1, "Yellow"),
                new Card(CardNameToImage("yellowtwo"), 2, "Yellow"),
                new Card(CardNameToImage("yellowthree"), 3, "Yellow"),
                new Card(CardNameToImage("yellowfour"), 4, "Yellow"),
                new Card(CardNameToImage("yellowfive"), 5, "Yellow"),
                new Card(CardNameToImage("yellowsix"), 6, "Yellow"),
                new Card(CardNameToImage("yellowseven"), 7, "Yellow"),
                new Card(CardNameToImage("yelloweight"), 8, "Yellow"),
                new Card(CardNameToImage("yellownine"), 9, "Yellow"),
                
                //Blue
                new Card(CardNameToImage("bluereverse"), -1, "Blue"),
                new Card(CardNameToImage("bluedrawtwo"), -2, "Blue"),
                new Card(CardNameToImage("blueblock"), -3, "Blue"),
                new Card(CardNameToImage("blueone"), 1, "Blue"),
                new Card(CardNameToImage("bluetwo"), 2, "Blue"),
                new Card(CardNameToImage("bluethree"), 3, "Blue"),
                new Card(CardNameToImage("bluefour"), 4, "Blue"),
                new Card(CardNameToImage("bluefive"), 5, "Blue"),
                new Card(CardNameToImage("bluesix"), 6, "Blue"),
                new Card(CardNameToImage("blueseven"), 7, "Blue"),
                new Card(CardNameToImage("blueeight"), 8, "Blue"),
                new Card(CardNameToImage("bluenine"), 9, "Blue"),
                
                //Red
                new Card(CardNameToImage("redreverse"), -1, "Red"),
                new Card(CardNameToImage("reddrawtwo"), -2, "Red"),
                new Card(CardNameToImage("redblock"), -3, "Red"),
                new Card(CardNameToImage("redone"), 1, "Red"),
                new Card(CardNameToImage("redtwo"), 2, "Red"),
                new Card(CardNameToImage("redthree"), 3, "Red"),
                new Card(CardNameToImage("redfour"), 4, "Red"),
                new Card(CardNameToImage("redfive"), 5, "Red"),
                new Card(CardNameToImage("redsix"), 6, "Red"),
                new Card(CardNameToImage("redseven"), 7, "Red"),
                new Card(CardNameToImage("redeight"), 8, "Red"),
                new Card(CardNameToImage("rednine"), 9, "Red"),

                //Green
                new Card(CardNameToImage("greenreverse"), -1, "Green"),
                new Card(CardNameToImage("greendrawtwo"), -2, "Green"),
                new Card(CardNameToImage("greenone"), 1, "Green"),
                new Card(CardNameToImage("greentwo"), 2, "Green"),
                new Card(CardNameToImage("greenthree"), 3, "Green"),
                new Card(CardNameToImage("greenfour"), 4, "Green"),
                new Card(CardNameToImage("greenfive"), 5, "Green"),
                new Card(CardNameToImage("greensix"), 6, "Green"),
                new Card(CardNameToImage("greenseven"), 7, "Green"),
                new Card(CardNameToImage("greeneight"), 8, "Green"),
                new Card(CardNameToImage("greennine"), 9, "Green"),
            };
            Random random = new Random();
            return cards[random.Next(0, cards.Count)];
        }

        public static Card ImageToCard(Image image)
        {
            for (int index = 0; index < Items.GameItem.player.Cards.Count; index++)
            {
                if (Items.GameItem.player.Cards[index].image == image)
                {
                    return Items.GameItem.player.Cards[index];
                }
            }

            return new Card();
        }

        public static RotateTransform rotate(int number)
        {
            RotateTransform rotate = new RotateTransform(number);
            rotate.CenterX = Table.topcard.image.Width / 2;
            rotate.CenterY = Table.topcard.image.Height / 2 + number;
            return rotate;
        }
    }

    public class Table
    {
        public static bool direction = true;
        public static int turn;
        public static List<Image> topcards = new List<Image>();
        public static List<int> topcardsrotateangle = new List<int>();
        public static Card topcard;
        public static Image draggedimage;
        public static Image deckimage;

        public static void DisplayScoreBoard()
        {
            Task.Delay(TimeSpan.FromSeconds(1.5)).ContinueWith(task =>
            {
                ScoreBoard board = new ScoreBoard();
                Items.GameItem.gamegrid.Children.Add(board);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static bool HasSomeoneWon()
        {
            for(int index = 0; index < CardHolder.AllCards.Count; index++)
            {
                if (CardHolder.AllCards[index].Cards.Count == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetNextTurn()
        {
            if (direction)
            {
                turn++;
                if (turn > CardHolder.AllCards.Count)
                {
                    turn = 1;
                }
            }

            else
            {
                turn--;
                if (turn < 1)
                {
                    turn = CardHolder.AllCards.Count;
                }
            }
        }

        public static int GetNextTurn()
        {
            int tempturn = 0;

            if (direction)
            {
                turn++;
                if (turn > CardHolder.AllCards.Count)
                {
                    tempturn = 1;
                }
            }

            else
            {
                turn--;
                if (turn < 1)
                {
                    tempturn = CardHolder.AllCards.Count;
                }
            }

            return tempturn;
        }

        public static void CheckForTurn()
        {
            RefreshVisuals();

            if (HasSomeoneWon())
            {
                DisplayScoreBoard();
            }

            else
            {
                if (turn != 1)
                {
                    ((Bot)CardHolder.AllCards[turn - 1]).PlayCard();
                }
            }
        }

        public static void TopCardsClear()
        {
            topcards.Clear();
            topcardsrotateangle.Clear();
        }

        public static void SetRandomTopCard()
        {
            topcard = Card.GetRandomCard();
            topcardsrotateangle.Add(0);
        }

        public static void AddToTopCards(int randomnumber)
        {
            Image image = Card.GetRandomCard().image;
            image.Width = image.Height = 150;
            image.Source = topcard.image.Source;
            topcards.Add(image);
            topcardsrotateangle.Add(randomnumber);
        }
        
        public static void RefreshVisuals()
        {
            if (Items.GameItem == null)
            {
                return;
            }

            Items.GameItem.player.RefreshCards(0);
            RefreshDeck(0);
            Playable();
            RefreshTopCards();
            RefreshBotsCards(0);
            RefreshDirection();
        }

        private static void RefreshDeck(int gap)
        {
            Image deck = new Image();
            deck = Card.CardNameToImage("cardbackground");
            deck.Width = deck.Height = 150;
            deck.MouseDown += Items.GameItem.DeckDown;
            deckimage = deck;
            Canvas.SetTop(deck, Items.GameItem.gamecanvas.ActualHeight / 3);
            Canvas.SetLeft(deck, Items.GameItem.gamecanvas.ActualWidth / 3.5 - gap);
            Items.GameItem.gamecanvas.Children.Add(deck);
            if (gap <= Items.GameItem.ActualGap * 3) { RefreshDeck(gap + Items.GameItem.ActualGap); }
        }

        private static void Playable()
        {
            if(turn != 1) { return; }
            Card topcard = Table.topcard;
            bool playable = false; ;
            if (topcard == null) { return; }

            for (int index = 0; index < Items.GameItem.player.Cards.Count; ++index)
            {
                if (Items.GameItem.player.Cards[index].color == "none" || topcard.color == Items.GameItem.player.Cards[index].color || topcard.number == Items.GameItem.player.Cards[index].number)
                {
                    Canvas.SetTop(Items.GameItem.player.Cards[index].image, (int)Items.GameItem.gamecanvas.ActualHeight - Items.GameItem.player.Cards[index].image.Width - 7);
                    playable = true;
                }
            }

            Image image = new Image();

            DropShadowEffect dropShadowEffect = new DropShadowEffect
            {
                Color = Colors.LightYellow,
                Direction = 135,
                ShadowDepth = 5,
                Opacity = 0.7
            };

            if (!playable && Table.turn == 1) { deckimage.Effect = dropShadowEffect; }
        }
        private static void RefreshBotsCards(int gap)
        {
            for (int index = 0; index < CardHolder.AllCards.Count; index++)
            {
                if (index == 0)
                {
                    continue;
                }
                CardHolder.AllCards[index].RefreshCards(gap);
            }
        }

        public static void RefreshDirection()
        {
            if (Settings.PlayerCount == 2) return;

            Image image = new Image();
            image.Source = Card.CardNameToImage(direction ? "arrowsclockwise" : "arrowscounterclockwise").Source;
            image.Width = image.Height = 150;
            Canvas.SetBottom(image, 180);
            Canvas.SetLeft(image, Items.GameItem.gamecanvas.ActualWidth / 2 - image.Width / 2);
            Items.GameItem.gamecanvas.Children.Add(image);
        }

        private static void RefreshTopCards()
        {
            if (topcards.Count > 20)
            {
                topcards.RemoveAt(0);
                topcardsrotateangle.RemoveAt(0);
            }

            for (int index = 0; index < topcards.Count; index++)
            {
                topcards[index].RenderTransform = Card.rotate(topcardsrotateangle[index]);
                Canvas.SetTop(topcards[index], Items.GameItem.gamecanvas.ActualHeight / 3);
                Canvas.SetLeft(topcards[index], Items.GameItem.gamecanvas.ActualWidth / 2 - 75);
                Items.GameItem.gamecanvas.Children.Remove(topcards[index]);
                Items.GameItem.gamecanvas.Children.Add(topcards[index]);
            }
        }
    }
}