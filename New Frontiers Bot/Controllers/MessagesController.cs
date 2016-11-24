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

                #region User type 'help'
                //Help command===================
                if (userMessage.ToLower().Contains("help"))
                {
                    string message = "**What you can ask me.**";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "**Key word:** *shopping list* to see your shopping list.";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "**Key word:** *add item* to start adding item to your shopping list.";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "**Key word:** *buy [number]* to cross out bought item from the shopping list";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    message = "**Key word:** *clear* to erase states / user data.";
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(message));
                    return Request.CreateResponse(HttpStatusCode.OK);
                }



                //===============================
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
                    message = "**Quantity**" + space + "**Item**" + space + "**Price**";
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