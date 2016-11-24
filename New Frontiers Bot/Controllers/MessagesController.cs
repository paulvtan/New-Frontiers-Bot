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

                #region DEAD CODE (CREATE)
                //CRUD MODEL--> Grocery Shopping List Features

                //Add new item to the shopping list. (C [Create])
                /*if ((userMessage.ToLower().Contains("add item")) && (userData.GetProperty<string>("itemName") == null || userData.GetProperty<string>("itemName") == ""))
                {
                    userData.SetProperty<string>("itemName", ""); //Set this propertie to empty string.
                    string itemName = userMessage.Substring(8);
                    itemName.Trim();
                    userData.SetProperty<string>("itemName", itemName); //Set the properties to item name. (Remember state)
                    userData.SetProperty<int>("quantity", 0);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    name = "**_" + itemName + "_**";
                    string str = "Item to add to shoppin list: ";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(str + name));
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("How many " + name + " would you like to buy?"));
                    return Request.CreateResponse(HttpStatusCode.OK);
                } else if (userData.GetProperty<int>("quantity") == 0 && (userData.GetProperty<string>("itemName") != null && userData.GetProperty<string>("itemName") != "")) //User input the quantity.
                {
                    userData.SetProperty<int>("quantity", 0); //Clear
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    int quantity = 0;
                    if (userMessage.ToLower().Equals("cancel"))
                    {
                        userData.SetProperty<string>("itemName", "");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You have cancel adding item to shopping list."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    try
                    {
                        message = userMessage.Trim();
                        quantity = int.Parse(message);
                        userData.SetProperty<int>("quantity", quantity);
                        //await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Does it get here?"));
                    } catch (Exception e) //if the operation fails
                    {
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, I didn't quite get that. How many would you like to buy? You can also *cancel* this operation."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    string itemName = userData.GetProperty<string>("itemName");
                    userData.SetProperty<int>("quantity", quantity); //Set the state to remember quantity.
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You are adding " + quantity + " **" + itemName + "** to the shopping list."));
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("How much does each item cost? Otherwise you can say **done** to finalise the additon or **cancel**."));
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state -> DELETE IF BREAK
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else if (userData.GetProperty<int>("quantity") != 0 && (userData.GetProperty<string>("itemName") != null && userData.GetProperty<string>("itemName") != "")) //User can input price or done or cancel.
                {
                    if (userMessage.ToLower().Contains("cancel")) //If the user decide to cancel
                    {
                        userData.SetProperty<string>("itemName", "");
                        userData.SetProperty<int>("quantity", 0);
                        userData.SetProperty<double>("individualPrice", 0);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You have cancel adding item to shopping list."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    //Variable to send to EasyTable
                    string itemName = userData.GetProperty<string>("itemName");
                    int quantity = userData.GetProperty<int>("quantity");
                    ShoppingList shoppingList;

                    if (userMessage.ToLower().Contains("done"))
                    {
                        if (userData.GetProperty<double>("individualPrice") != 0)
                        {
                            double individualPrice = userData.GetProperty<double>("individualPrice");
                            double sumPrice = individualPrice * quantity;
                            shoppingList = new ShoppingList()
                            {
                                ItemName = itemName,
                                Quantity = quantity,
                                IndividualPrice = individualPrice,
                                SumPrice = sumPrice
                            };
                        }
                        else //Adding to the shopping list table without individual price and sum.
                        {
                            shoppingList = new ShoppingList()
                            {
                                ItemName = itemName,
                                Quantity = quantity,
                            };

                        }
                        await AzureManager.AzureManagerInstace.AddShoppingList(shoppingList);

                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("The item has been added successfully to shopping list!"));

                        //Clear the states back to normal.
                        userData.SetProperty<string>("itemName", "");
                        userData.SetProperty<int>("quantity", 0);
                        userData.SetProperty<double>("individualPrice", 0);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        return Request.CreateResponse(HttpStatusCode.OK);

                    } else //If the user decides to input individual price for the item.
                    {
                        try
                        {
                            message = userMessage.Trim();
                            double individualPrice = double.Parse(message);
                            userData.SetProperty<double>("individualPrice", individualPrice);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("You are adding " + quantity + " **" + itemName + "** each cost $**" + individualPrice +  "** to the shopping list."));
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Would you like to add the following?"));
                            return Request.CreateResponse(HttpStatusCode.OK);
                        } catch (Exception e)
                        {
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Sorry, I didn't quite get that. How much is each of this item? You can also add this item using *done* or *cancel* this operation."));
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                    }


                }*/
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
                    message = "**Quantity**" + space + "**Item Name**" + space + "**Price**";
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
                        individualPrice = int.Parse(temp);
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
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Something went wrong, could you please try again?"));
                        userData.SetProperty<bool>("isAdding", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("isAdding", false); //Reset this back to false
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); //Update state
                    #region show shopping list again
                    //=====================================================
                    //Show 'shopping list'.(Show all the items currently in the shopping list) [R (Read)]================================
                    
                    
                        List<ShoppingList> shoppingLists = await AzureManager.AzureManagerInstace.GetShoppingList();
                        message = ""; //Clear message to be blank  
                        string space = "     ";
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("**Your Grocery Shopping List**"));
                        int count = 1;
                        foreach (ShoppingList l in shoppingLists)
                        {
                            string individualPrice = "";
                            string sumPrice = "";
                            if (l.IndividualPrice != 0) { individualPrice = "($" + l.IndividualPrice + " each)"; }
                            if (l.IndividualPrice != 0) { sumPrice = "Total: $" + l.SumPrice; }

                            message = "**" + count + ".**  " + l.Quantity + " x " + l.ItemName + space + individualPrice + space + sumPrice;
                            if (l.StrikeOut) //If the row has strikeOut field marked as 'true', displayed message as strikeout.
                            {
                                message = "**" + count + ".**  ~~" + l.Quantity + " x " + l.ItemName + space + individualPrice + space + sumPrice + "~~";
                            }
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                            count += 1;
                        }

                        
                    
                    #endregion
                    return Request.CreateResponse(HttpStatusCode.OK);
                }


                //====================================================
                #endregion

                #region Show Shopping List (Read)
                //=====================================================
                //Show 'shopping list'.(Show all the items currently in the shopping list) [R (Read)]================================
                if (userMessage.ToLower().Contains("shopping list"))
                {
                    List<ShoppingList> shoppingLists = await AzureManager.AzureManagerInstace.GetShoppingList();
                    message = ""; //Clear message to be blank  
                    string space = "     ";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("**Your Grocery Shopping List**"));
                    int count = 1;
                    foreach (ShoppingList l in shoppingLists)
                    {
                        string individualPrice = "";
                        string sumPrice = "";
                        if (l.IndividualPrice != 0) { individualPrice = "($" + l.IndividualPrice + " each)";}
                        if (l.IndividualPrice != 0) { sumPrice = "Total: $" + l.SumPrice; }

                        message = "**" + count + ".**  " + l.Quantity + " x " + l.ItemName + space + individualPrice + space + sumPrice;
                        if (l.StrikeOut) //If the row has strikeOut field marked as 'true', displayed message as strikeout.
                        {
                            message = "**" + count + ".**  ~~" + l.Quantity + " x " + l.ItemName + space + individualPrice + space + sumPrice + "~~";
                        }
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                        count += 1;
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                //==================================================================================================================
                #endregion

                #region Strike out item in the shopping list (Update)
                //Strike out item in the shopping list (update)

                //---------------------------------------------
                #endregion
                //--------------------------------------------------------------

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