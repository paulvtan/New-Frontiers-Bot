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
            double total = 0;
            foreach (ShoppingList l in lists)
            {
                
                string labelPrice = "$" + l.IndividualPrice + "";
                string labelQuantity = l.Quantity + "";
                string labelName = count + ". " + l.ItemName + " (x " + labelQuantity + ")";
                string labelSumPrice = l.SumPrice + "";
                string choice = cardUrlCross;
                if (!l.StrikeOut) { total += l.SumPrice; };
                if (l.StrikeOut) { choice = cardUrlTick; }
                ReceiptItem x = new ReceiptItem(labelName, price: labelPrice + " (" + labelSumPrice + ")", quantity: labelQuantity, image: new CardImage(url: choice));
                items.Add(x);
                count++;
            }

            var card = new ReceiptCard
            {
                Title = "Shopping List",
                Items = items,
                Total = "$" + total
            };

            return card.ToAttachment();

        }
        #endregion

    }
}