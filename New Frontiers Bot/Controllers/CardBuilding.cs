using Microsoft.Bot.Connector;
using New_Frontiers_Bot.DataModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Frontiers_Bot.Controllers
{
    public class CardBuilding : MessagesController
    {
        #region Name Confirm Card
        //This card confirm the user selected name.
        public static Activity getNameCard(string userName, Activity activity) 
        {
            Activity nameConfirmCard = activity.CreateReply("Would you like to be called\n" + userName + "?");
            nameConfirmCard.Recipient = activity.From;
            nameConfirmCard.Type = "message";
            nameConfirmCard.Attachments = new List<Attachment>();

            //Build card action
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction yesButton = new CardAction()
            {
                Value = "Yes, Call me " + userName,
                Type = "imBack",
                Title = "Yes"
            };

            CardAction noButton = new CardAction()
            {
                Value = "No",
                Type = "imBack",
                Title = "No"
            };
            cardButtons.Add(yesButton);
            cardButtons.Add(noButton);

            //Create thumbnailCard
            ThumbnailCard card = new ThumbnailCard()
            {
                Buttons = cardButtons
            };

            Attachment cardAttacthment = card.ToAttachment(); //Turn the card into attachment to attach to message.
            nameConfirmCard.Attachments.Add(cardAttacthment);
            return nameConfirmCard; 
        }
        #endregion

        #region ShoppingList Card
        //Return card attachment so you can attach this to the message back to user.
        public static Attachment GetShoppingListCard(List<ShoppingList> lists)
        {
            string cardUrlTick = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/GreenTick.png";
            string cardUrlCross = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/Cross.png";
            List<ReceiptItem> items = new List<ReceiptItem>();
            int count = 1;
            double tempTotal = 0;
            foreach (ShoppingList l in lists)
            {
                
                string labelPrice = "$" + l.IndividualPrice + "";
                string labelQuantity = l.Quantity + "";
                string labelName = count + ". " + l.ItemName + " (x " + labelQuantity + ")";
                string labelSumPrice = l.SumPrice + "";
                string choice = cardUrlCross;
                if (!l.StrikeOut) { tempTotal += l.SumPrice; };
                if (l.StrikeOut) { choice = cardUrlTick; }
                ReceiptItem x = new ReceiptItem(labelName, price: labelPrice + " (" + labelSumPrice + ")", quantity: labelQuantity, image: new CardImage(url: choice));
                items.Add(x);
                count++;
            }
            string total = "$" + tempTotal;
            if (items.Count() == 0) { total = "The list is empty"; }

            CardAction addItemButton = new CardAction()
            {
                Type = "imBack",
                Title = "Add More Item",
                Value = "add item"
            };

            CardAction buyItemButton = new CardAction()
            {
                Type = "imBack",
                Title = "Mark Item As Bought",
                Value = "mark item"
            };

            CardAction deleteListButton = new CardAction()
            {
                Type = "imBack",
                Title = "Delete This List",
                Value = "delete"
            };
            
            var card = new ReceiptCard
            {
                Title = "Shopping List",
                Items = items,
                Total = total,
                Buttons = new List<CardAction>
                {
                    addItemButton,
                    buyItemButton,
                    deleteListButton
                }
            };

            return card.ToAttachment();

        }
        #endregion

        #region Help Card
        //Return card attachment so you can attach this to the message back to user.
        public static Attachment GetHelpCard()
        {
            string shoppingCartIconUrl = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/shoppingCart.png";
            string plusIconUrl = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/plus.png";
            string tickIconUrl = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/GreenTick.png";
            string deleteIconUrl = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/delete.png";
            string clearIconUrl = "https://raw.githubusercontent.com/paulvtan/New-Frontiers-Bot/master/Remove%20User%20Male-64.png";
            List<ReceiptItem> items = new List<ReceiptItem>();
            ReceiptItem x = new ReceiptItem("1. Display Shopping List", image: new CardImage(url: shoppingCartIconUrl));
            items.Add(x);
            x = new ReceiptItem("2. Add Item", image: new CardImage(url: plusIconUrl));
            items.Add(x);
            x = new ReceiptItem("3. Mark As Paid", image: new CardImage(url: tickIconUrl));
            items.Add(x);
            x = new ReceiptItem("4. Delete This List", image: new CardImage(url: deleteIconUrl));
            items.Add(x);
            x = new ReceiptItem("5. Clear User Data", image: new CardImage(url: clearIconUrl));
            items.Add(x);

            CardAction commandPanel = new CardAction()
            {
                Type = "imBack",
                Title = "Open Command Panel",
                Value = "command panel"
            };

            var card = new ReceiptCard
            {
                Title = "What you can ask me",
                Items = items,
                Total = "5 Commands",
                Buttons = new List<CardAction>
                {
                    commandPanel
                }
            };

            return card.ToAttachment();

        }
        #endregion
    }
}