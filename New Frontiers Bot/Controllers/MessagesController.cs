using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;
using New_Frontiers_Bot.DataModels;

namespace New_Frontiers_Bot
{
    
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// 
        //General name space holder
        public string name = "";
        public string message = "";

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            

            if (activity.Type == ActivityTypes.Message)
            {
                
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var userMessage = activity.Text;
                List<CardAction> buttons = new List<CardAction>();
                Activity reply = activity.CreateReply("");
                HeroCard heroCard;
                Attachment attachment;

                #region Setting up the state Client
                //1. Setting up the State Client==========================

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id); //Accessing the bot data of a user.
                //========================================================
                #endregion

                #region Clear user state using 'clear' command
                //2. Clear the user state=================================
                if (userMessage.ToLower().Contains("clear"))
                {
                    string message = "Your user data has been cleared!";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id); //Deleting the user state
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                //=====================================================
                #endregion
                  
                #region Learning the user name
                //3. (First time) Greeting User and Learn the user name.===============
                if (!userData.GetProperty<bool>("SendGreeting")) //First time greeting the user.
                {
                    string welcomeMessage = "Hi! I am the New Frontiers Assistance. What would you like to be known as?";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(welcomeMessage));
                    userData.SetProperty<bool>("SendGreeting", true); //Set the properties of sendGreeting to true as already greet once.
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    //If the name for the user doesn't exist yet. 
                    if (userData.GetProperty<string>("UserName") == null || userData.GetProperty<string>("UserName") == "")
                    {
                        //(User input thier prefer name)
                        //If the user name doesn't exist. The nameConfirmCard will be showned.
                        string userName = userMessage.Trim(); //user type in thier prefer name
                        Activity nameConfirmCard = Controllers.CardBuilding.getNameCard(userName, activity);
                        userData.SetProperty("UserName", userName); //Set the UserName to selecting mode waiting for yes or no. 
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        await connector.Conversations.ReplyToActivityAsync(nameConfirmCard);
                        return Request.CreateResponse(HttpStatusCode.OK);

                    } else if (userData.GetProperty<string>("UserName") != "" && (!userData.GetProperty<bool>("userNameExist"))) //After the name card is showned.
                    {
                        //If the user hit the 'yes' on the nameConfirmCard
                        if (activity.Text.ToLower().Contains("call me") || activity.Text.ToLower().Contains("yes")) {
                            string name = userData.GetProperty<string>("UserName");
                            name.Trim();
                            userData.SetProperty("UserName", name); //Set the user name
                            userData.SetProperty("userNameExist", true); //so that the bot knows the user name is already assigned and won't ask again.
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Nice to meet you *" + name + "!*"));
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You can type *help* to see what I can do for you."));

                            #region Help button
                            Activity reply1 = activity.CreateReply("");
                            reply1.Recipient = activity.From;
                            reply1.Type = "message";
                            reply1.Attachments = new List<Attachment>();
                            List<CardAction> buttons1 = new List<CardAction>();

                            CardAction helpButton1 = new CardAction()
                            {
                                Type = "imBack",
                                Title = "Help",
                                Value = "help"
                            };
                            buttons1.Add(helpButton1);
                            var heroCard1 = new HeroCard()
                            {
                                Title = "Press 'Help' to Learn More.",
                                Subtitle = "",
                                Buttons = buttons1
                            };

                            Attachment attachment1 = heroCard1.ToAttachment();
                            reply1.Attachments.Add(attachment1);
                            await connector.Conversations.ReplyToActivityAsync(reply1); //Finally reply to user.
                            #endregion

                            return Request.CreateResponse(HttpStatusCode.OK);

                        } else
                        //If the user select 'No' on the nameConfirmedCard
                        {
                            userData.SetProperty("UserName", ""); //Set the userName back to null, and ask for the name again.
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("What would you like to be called?"));
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                    }
                }
                //=====================================================
                #endregion

                #region User Type 'help'
                if (userMessage.ToLower().Contains("help"))
                {

                    Activity helpCard = activity.CreateReply("");
                    helpCard.Recipient = activity.From;
                    helpCard.Type = "message";
                    helpCard.Attachments = new List<Attachment>();
                    Attachment helpCardAttatchment = Controllers.CardBuilding.GetHelpCard();
                    helpCard.Attachments.Add(helpCardAttatchment);
                    await connector.Conversations.ReplyToActivityAsync(helpCard); //Finally reply to user.
                    return Request.CreateResponse(HttpStatusCode.OK);

                }
                #endregion

                #region 'help' Command Panel
                if (userMessage.ToLower().Contains("command panel"))
                {
                    reply = activity.CreateReply("");
                    reply.Recipient = activity.From;
                    reply.Type = "message";
                    reply.Attachments = new List<Attachment>();
                    buttons = new List<CardAction>();

                    CardAction shoppingListButton = new CardAction()
                    {
                        Type = "imBack",
                        Title = "1. Show Shopping List",
                        Value = "shopping list"
                    };
                    buttons.Add(shoppingListButton);

                    CardAction addItemButton = new CardAction()
                    {
                        Type = "imBack",
                        Title = "2. Add Item to Shopping List",
                        Value = "add item"
                    };
                    buttons.Add(addItemButton);

                    CardAction markItemButton = new CardAction()
                    {
                        Type = "imBack",
                        Title = "3. Mark Item Paid",
                        Value = "mark item"
                    };
                    buttons.Add(markItemButton);

                    CardAction deleteListButton = new CardAction()
                    {
                        Type = "imBack",
                        Title = "4. Delete Shopping List",
                        Value = "delete"
                    };
                    buttons.Add(deleteListButton);

                    CardAction clearUserDataButton = new CardAction()
                    {
                        Type = "imBack",
                        Title = "5. Clear User Data",
                        Value = "clear"
                    };
                    buttons.Add(clearUserDataButton);

                    heroCard = new HeroCard()
                    {
                        Title = "What would you like to do?",
                        Subtitle = "",
                        Buttons = buttons
                    };

                    attachment = heroCard.ToAttachment();
                    reply.Attachments.Add(attachment);
                    await connector.Conversations.ReplyToActivityAsync(reply); //Finally reply to user.
                    return Request.CreateResponse(HttpStatusCode.OK);


                }

                #endregion


                //CRUDE (Grocery Shopping List Features)---------------------------------
                //Connected to the EasyTable
                #region Add row to the shopping list (Create)
                //====================================================
                //Add new row to the shopping list.
                if (userMessage.ToLower().Contains("add item") && (!userData.GetProperty<bool>("isAdding")))
                {
                    string space = "     ";
                    message = "Add item to the shopping list by specifying the following format.";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "**Quantity**" + space + "**Item**" + space + "**Price $**";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "Example: **5 Apple 1.5**";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    userData.SetProperty<bool>("isAdding", true); //Set this is true so that next input will update.
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    return Request.CreateResponse(HttpStatusCode.OK);
                } else if (userData.GetProperty<bool>("isAdding") && userMessage != "")
                {
                    try
                    { 
                        string itemName = "";
                        int quantity = 0;
                        double individualPrice = 0;
                        double sumPrice = 0;
                        string[] userInput = userMessage.Split();
                        itemName = userInput[1] ?? "";
                        quantity = int.Parse(userInput[0]);
                        string temp = userInput[2] ?? "0";
                        individualPrice = double.Parse(temp);
                        sumPrice = individualPrice * quantity;
                        message = "**" + quantity + "** x " + itemName + " (each cost **$" + individualPrice + "**) Total: **$" + sumPrice + "** has been added to your shopping list.";
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                        ShoppingList shoppingList = new ShoppingList()
                        {
                            ItemName = itemName,
                            Quantity = quantity,
                            IndividualPrice = individualPrice,
                            SumPrice = sumPrice
                        };

                        await AzureManager.AzureManagerInstace.AddShoppingList(shoppingList);
                       
                    } catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, I didn't understand what you want to add, could you please try again?"));
                        userData.SetProperty<bool>("isAdding", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("isAdding", false); //Reset this back to false
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    #region ShoppingListCard Display Again 
                    Attachment shoppingListCardAttachment;
                    try
                    {
                        List<ShoppingList> shoppingLists1 = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                        shoppingListCardAttachment = Controllers.CardBuilding.GetShoppingListCard(shoppingLists1);
                    }
                    catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " Something went wrong."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    Activity shoppingListCard = activity.CreateReply("");
                    shoppingListCard.Recipient = activity.From;
                    shoppingListCard.Type = "message";
                    shoppingListCard.Attachments = new List<Attachment>();
                    shoppingListCard.Attachments.Add(shoppingListCardAttachment);

                    await connector.Conversations.ReplyToActivityAsync(shoppingListCard); //Finally reply to user.




                    #endregion
                    return Request.CreateResponse(HttpStatusCode.OK);
                }


                //====================================================
                #endregion

                #region ShoppingListCard Display (Read)
                if (userMessage.ToLower().Contains("shopping list"))
                {
                    Attachment shoppingListCardAttachment;
                    try
                    {
                        List<ShoppingList> shoppingLists1 = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                        shoppingListCardAttachment = Controllers.CardBuilding.GetShoppingListCard(shoppingLists1);
                    }
                    catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " Something went wrong."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    Activity shoppingListCard = activity.CreateReply("");
                    shoppingListCard.Recipient = activity.From;
                    shoppingListCard.Type = "message";
                    shoppingListCard.Attachments = new List<Attachment>();
                    shoppingListCard.Attachments.Add(shoppingListCardAttachment);

                    await connector.Conversations.ReplyToActivityAsync(shoppingListCard); //Finally reply to user.
                    return Request.CreateResponse(HttpStatusCode.OK);

                }
                #endregion

                #region Mark helper function (Hero Card to Strike out item)
                if (userMessage.ToLower().Equals("mark item"))
                {
                    List<ShoppingList> shoppingLists;
                    try
                    {
                        shoppingLists = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                    }
                    catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " Something went wrong."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    bool allStrikedOut = true; //Set to check if all the items are striked out already, if so it will not enter mark screen
                    foreach(ShoppingList l in shoppingLists)
                    {
                        if (l.StrikeOut == false) { allStrikedOut = false; }
                    }
                    if (allStrikedOut)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You have already bought all the items on the list.")); 
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    if (shoppingLists.Count() == 0) //If the list is not empty or already been all striked out.
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Your Shopping List is Currently Empty."));
                        #region 'help' Command Panel

                        reply = activity.CreateReply("");
                        reply.Recipient = activity.From;
                        reply.Type = "message";
                        reply.Attachments = new List<Attachment>();
                        buttons = new List<CardAction>();

                        CardAction shoppingListButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "1. Show Shopping List",
                            Value = "shopping list"
                        };
                        buttons.Add(shoppingListButton);

                        CardAction addItemButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "2. Add Item to Shopping List",
                            Value = "add item"
                        };
                        buttons.Add(addItemButton);

                        CardAction markItemButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "3. Mark Item Paid",
                            Value = "mark item"
                        };
                        buttons.Add(markItemButton);

                        CardAction deleteListButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "4. Delete Shopping List",
                            Value = "delete"
                        };
                        buttons.Add(deleteListButton);

                        CardAction clearUserDataButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "5. Clear User Data",
                            Value = "clear"
                        };
                        buttons.Add(clearUserDataButton);

                        heroCard = new HeroCard()
                        {
                            Title = "What would you like to do?",
                            Subtitle = "",
                            Buttons = buttons
                        };

                        attachment = heroCard.ToAttachment();
                        reply.Attachments.Add(attachment);
                        await connector.Conversations.ReplyToActivityAsync(reply); //Finally reply to user.





                        #endregion
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    //Creating the item selection card (user can select which one to strink out)
                    reply = activity.CreateReply("");
                    reply.Recipient = activity.From;
                    reply.Type = "message";
                    reply.Attachments = new List<Attachment>();
                    buttons = new List<CardAction>();
                    int count = 1;
                    foreach (ShoppingList l in shoppingLists)
                    {
                        message = count + "." + l.ItemName + " (x" + l.Quantity + ") $" + l.SumPrice;
                        CardAction button = new CardAction()
                        {
                            Type = "imBack",
                            Title = message,
                            Value = "buy " + count 
                        };
                        buttons.Add(button);
                        count++;
                    }
                    heroCard = new HeroCard()
                    {
                        Title = "Select the following.",
                        Subtitle = "",
                        Buttons = buttons
                    };

                    attachment = heroCard.ToAttachment();
                    reply.Attachments.Add(attachment);
                    await connector.Conversations.ReplyToActivityAsync(reply); //Finally reply to user.
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                #endregion

                #region Strike out item in the shopping list (Update)
                //Strike out item in the shopping list (update)=======================
                if (userMessage.ToLower().Contains("buy")) 
                {
                    string itemName;
                    int listIndexSelect;
                    ShoppingList updatedList;
                    try
                    {
                        List<ShoppingList> shoppingLists1 = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                        string userSelection = userMessage.Substring(3);
                        listIndexSelect = int.Parse(userSelection.Trim());
                        itemName = shoppingLists1[listIndexSelect - 1].ItemName;
                        updatedList = shoppingLists1[listIndexSelect - 1];
                        updatedList.StrikeOut = true;
                    }
                    catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " I did not understand what you mean."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(listIndexSelect + ". ~~" + itemName + "~~ has been bought."));
                    await AzureManager.AzureManagerInstace.UpdateShoppingList(updatedList); //Updated the row to the table
                    #region ShoppingListCard Display Again 
                    Attachment shoppingListCardAttachment;
                    try
                    {
                        List<ShoppingList> shoppingLists1 = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                        shoppingListCardAttachment = Controllers.CardBuilding.GetShoppingListCard(shoppingLists1);
                    }
                    catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " Something went wrong."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    Activity shoppingListCard = activity.CreateReply("");
                    shoppingListCard.Recipient = activity.From;
                    shoppingListCard.Type = "message";
                    shoppingListCard.Attachments = new List<Attachment>();
                    shoppingListCard.Attachments.Add(shoppingListCardAttachment);

                    await connector.Conversations.ReplyToActivityAsync(shoppingListCard); //Finally reply to user.




                    #endregion
                    return Request.CreateResponse(HttpStatusCode.OK);

                }
                //====================================================================
                #endregion


                #region Delete the shopping list (Delete)
                if (userMessage.ToLower().Contains("delete"))
                {
                    try
                    {
                        List<ShoppingList> shoppingLists1 = await AzureManager.AzureManagerInstace.GetShoppingList(); //Grabbing all the rows from the table.
                        foreach(ShoppingList list in shoppingLists1)
                        {
                            await AzureManager.AzureManagerInstace.DeleteShoppingList(list);
                        }
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Your grocery shopping list has been deleted."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    } catch (Exception)
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, " + userData.GetProperty<string>("userName") + " something went wrong."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }
                #endregion
                //-----------------------------------------------------------------------
                //General

                #region Reply to Users saying "hi, hey, hello" and unrecognised command
                //=====================================================
                //if user says "hi"
                if (userMessage.ToLower().Contains("hi") || userMessage.ToLower().Contains("hello") || userMessage.ToLower().Contains("hey"))
                {
                    name = userData.GetProperty<string>("UserName");
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Hello, " + name + "."));
                    return Request.CreateResponse(HttpStatusCode.OK);
                }


                //Reply back to user if no command match===============
                name = userData.GetProperty<string>("UserName");
                await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry I did not understand what you said " + name + "."));
                await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You can type *help* to see what I can do for you."));

                #region Help button
                reply = activity.CreateReply("");
                reply.Recipient = activity.From;
                reply.Type = "message";
                reply.Attachments = new List<Attachment>();
                buttons = new List<CardAction>();

                CardAction helpButton = new CardAction()
                {
                    Type = "imBack",
                    Title = "Help",
                    Value = "help"
                };
                buttons.Add(helpButton);
                heroCard = new HeroCard()
                {
                    Title = "Press 'Help' to Learn More.",
                    Subtitle = "",
                    Buttons = buttons
                };

                attachment = heroCard.ToAttachment();
                reply.Attachments.Add(attachment);
                await connector.Conversations.ReplyToActivityAsync(reply); //Finally reply to user.
                #endregion

                return Request.CreateResponse(HttpStatusCode.OK);
                //=====================================================
                #endregion


            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}