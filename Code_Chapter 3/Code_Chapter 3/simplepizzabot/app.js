var restify = require('restify');
var builder = require('botbuilder');

// Setup Restify Server
var server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, function () {
   console.log('%s listening to %s', server.name, server.url); 
});
  
// Create chat bot
var connector = new builder.ChatConnector({
    appId: process.env.MICROSOFT_APP_ID,
    appPassword: process.env.MICROSOFT_APP_PASSWORD
});

// Setup bot and root waterfall
var bot = new builder.UniversalBot(connector, [
    function (session) {
        session.send("Hello... I'm a pizza bot.");
        session.beginDialog('rootMenu');
    },
    function (session, results) {
        session.endConversation("Goodbye until next time...");
    }
]);

server.post('/api/messages', connector.listen());

// Add root menu dialog
bot.dialog('rootMenu', [
    function (session) {
        builder.Prompts.choice(session, "Choose an option:", 'Select Base|Select Toppings|Select Sides|Confirm Order|Order Summary');
    },
    function (session, results) {
        switch (results.response.index) {
            case 0:
                session.beginDialog('basedialog');
                break;
            case 1:
                session.beginDialog('toppingsdialog');
                break;
            case 2:
                session.beginDialog('sidesdialog');
                break;
            case 3:
                session.beginDialog('confirmorder');
                break;
            case 4:
                session.beginDialog('ordersummary');
                break;
            default:
                session.endDialog();
                break;
        }
    },
    function (session) {
        // Reload menu
        session.replaceDialog('rootMenu');
    }
]).reloadAction('showMenu', null, { matches: /^(menu|back)/i });

// select base
bot.dialog('basedialog', [
    function (session, args) {
        builder.Prompts.choice(session, "Choose Thin Crust, Cheese Burst or Classic Hand Tossed", "thincrust|cheeseburst|classichandtossed", { listStyle: builder.ListStyle.button })
    },
    function (session, results) {
            session.userData.base = results.response.entity;
            session.endDialog("It's %s.", results.response.entity);
    }
]);

// select toppings
bot.dialog('toppingsdialog', [
    function (session, args) {
        builder.Prompts.text(session, "Choose your toppings from Olives, Jalapeno, Onion, Bell Pepper, Corn");
    },
    function (session, results) {
            session.userData.toppings = results.response;
            session.endDialog("It's %s.", results.response);
    }
]);

// select sides
bot.dialog('sidesdialog', [
    function (session, args) {
        builder.Prompts.choice(session, "Choose Potato Wedges, Garlic Bread", "potatowedges|garlicbread", { listStyle: builder.ListStyle.auto })
    },
    function (session, results) {
            session.userData.sides = results.response.entity;
            session.endDialog("It's %s.", results.response.entity);
    }
]);

// confirm order
bot.dialog('confirmorder', [
    function (session, args) {
        builder.Prompts.confirm(session, "Can I confirm your order?"); 
    },
    function (session, results) {
            session.endDialog("It's %s.", results.response);
    }
]);

// order summary - bot state
bot.dialog('ordersummary', [
    function (session, args) {
        var msg = new builder.Message()
                .address(session.message.address)
                .attachments([
                         new builder.HeroCard(session)
                         .title("Pizza Bot")
                         .subtitle("Order Summary")
                         .text("Pizza Base: " + session.userData.base + " Pizza Toppings: " + session.userData.toppings + " Sides: " + session.userData.sides) 
                         .images([
                            builder.CardImage.create(session, "http://images.clipartpanda.com/pizza-clipart-pizza-clipart-1.jpg")
                        ])
                        .tap(builder.CardAction.openUrl(session, "https://docs.botframework.com/en-us/node/builder/overview/#navtitle"))]);

       // Send message through chat bot
       bot.send(msg);
    },
    function (session, results) {
       session.endDialog();
    }
]);
