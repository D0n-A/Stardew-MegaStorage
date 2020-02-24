﻿using furyx639.Common;
using MegaStorage.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MegaStorage.Framework.Interface
{
    public class CustomItemGrabMenu : ItemGrabMenu
    {
        /*********
        ** Fields
        *********/
        public static readonly Dictionary<string, Vector2> Categories = new Dictionary<string, Vector2>()
        {
            {"All", Vector2.Zero },
            {"Crops", new Vector2(640, 80)},
            {"Seeds", new Vector2(656, 64)},
            {"Materials", new Vector2(672, 64)},
            {"Cooking", new Vector2(688, 64)},
            {"Fishing", new Vector2(640, 64)},
            {"Misc", new Vector2(672, 80)}
        };

        private protected ClickableTextureComponent UpArrow;
        private protected ClickableTextureComponent DownArrow;
        private const int Rows = 6;
        private const int ItemsPerRow = 12;
        private const int Capacity = ItemsPerRow * Rows;
        private static int MenuWidth => ItemsPerRow * TileSize;
        private static int MenuHeight => (Rows + 3) * TileSize + (Rows + 1) * 4 + 56;
        private static int BorderWidth => IClickableMenu.borderWidth;
        private static int SpaceToClearSideBorder => IClickableMenu.spaceToClearSideBorder;
        private static int SpaceToClearTopBorder => IClickableMenu.spaceToClearTopBorder;
        private static int TileSize => Game1.tileSize;
        private readonly CustomChest _customChest;
        private CustomInventoryMenu _itemsToGrabMenu;
        private CustomInventoryMenu _inventory;

        private Item SourceItem => _sourceItemReflected.GetValue();
        private readonly IReflectedField<Item> _sourceItemReflected;
        private TemporaryAnimatedSprite Poof { set => _poofReflected.SetValue(value); }
        private readonly IReflectedField<TemporaryAnimatedSprite> _poofReflected;
        private behaviorOnItemSelect BehaviorFunction => _behaviorFunctionReflected.GetValue();
        private readonly IReflectedField<behaviorOnItemSelect> _behaviorFunctionReflected;

        /*********
        ** Public methods
        *********/
        public CustomItemGrabMenu(CustomChest customChest)
            : base(CommonHelper.NonNull(customChest).items, customChest)
        {
            initialize(
                (Game1.viewport.Width - MenuWidth) / 2 - BorderWidth,
                (Game1.viewport.Height - MenuHeight) / 2 - BorderWidth,
                MenuWidth + BorderWidth * 2,
                MenuHeight + BorderWidth * 2);

            _customChest = customChest;
            _sourceItemReflected = MegaStorageMod.Instance.Helper.Reflection.GetField<Item>(this, "sourceItem");
            _poofReflected = MegaStorageMod.Instance.Helper.Reflection.GetField<TemporaryAnimatedSprite>(this, "poof");
            _behaviorFunctionReflected = MegaStorageMod.Instance.Helper.Reflection.GetField<behaviorOnItemSelect>(this, "behaviorFunction");

            // Shift yPosition down if too high up
            if (yPositionOnScreen < BorderWidth + SpaceToClearTopBorder)
            {
                yPositionOnScreen = BorderWidth + SpaceToClearTopBorder;
            }

            // Shift xPosition right if too far left
            if (xPositionOnScreen < 0)
            {
                xPositionOnScreen = 0;
            }

            allClickableComponents = new List<ClickableComponent>();

            SetupInventoryMenu();
            SetupItemsMenu();
            SetControllerSupport();
        }
        private void SetControllerSupport()
        {
            if (Game1.options.SnappyMenus)
            {
                foreach (var cc in _itemsToGrabMenu.inventory.Where(cc => !(cc is null)))
                {
                    cc.myID += 53910;
                    cc.upNeighborID += 53910;
                    cc.rightNeighborID += 53910;
                    cc.downNeighborID = -7777;
                    cc.leftNeighborID += 53910;
                    cc.fullyImmutable = true;
                }
            }

            for (var index = 0; index < 12; ++index)
            {
                if (_inventory.inventory.Count >= 12)
                {
                    _inventory.inventory[index].upNeighborID = discreteColorPickerCC is null || _itemsToGrabMenu.inventory.Count > index
                        ? _itemsToGrabMenu.inventory.Count > index ? 53910 + index : 53910
                        : 4343;
                }

                if (!(discreteColorPickerCC is null) && _itemsToGrabMenu.inventory.Count > index)
                {
                    _itemsToGrabMenu.inventory[index].upNeighborID = 4343;
                }
            }

            for (var index = 0; index < 36; ++index)
            {
                if (_inventory.inventory.Count <= index)
                {
                    continue;
                }

                _inventory.inventory[index].upNeighborID = -7777;
                _inventory.inventory[index].upNeighborImmutable = true;
            }

            if (!(trashCan is null) && _inventory.inventory.Count >= 12 && !(_inventory.inventory[11] is null))
            {
                _inventory.inventory[11].rightNeighborID = 5948;
            }

            if (!(trashCan is null))
            {
                trashCan.leftNeighborID = 11;
            }

            if (!(okButton is null))
            {
                okButton.leftNeighborID = 11;
            }

            for (var i = 0; i < 12; i++)
            {
                var item = _inventory.inventory[i];
                if (!(item is null))
                {
                    item.upNeighborID = 53910 + 60 + i;
                }
            }

            var rightItems =
                Enumerable.Range(0, 6)
                    .Select(i => _itemsToGrabMenu.inventory.ElementAt(i * 12 + 11))
                    .ToList();

            for (var i = 0; i < rightItems.Count; ++i)
            {
                rightItems[i].rightNeighborID = i switch
                {
                    0 => UpArrow.myID,
                    1 => UpArrow.myID,
                    2 => colorPickerToggleButton.myID,
                    3 => organizeButton.myID,
                    4 => DownArrow.myID,
                    5 => DownArrow.myID,
                    6 => DownArrow.myID,
                    _ => organizeButton.myID
                };
            }

            if (!(colorPickerToggleButton is null))
            {
                colorPickerToggleButton.leftNeighborID = rightItems[2].myID;
                colorPickerToggleButton.upNeighborID = UpArrow.myID;
                UpArrow.rightNeighborID = colorPickerToggleButton.myID;
            }

            UpArrow.leftNeighborID = rightItems[0].myID;
            DownArrow.rightNeighborID = organizeButton.myID;
            DownArrow.leftNeighborID = rightItems[4].myID;
            DownArrow.downNeighborID = rightItems[5].myID;
            organizeButton.leftNeighborID = rightItems[3].myID;
            organizeButton.downNeighborID = DownArrow.myID;
            /*
            if (ModConfig.Instance.EnableCategories)
            {
                var leftItems =
                    Enumerable.Range(0, 6)
                        .Select(i => _itemsToGrabMenu.inventory.ElementAt(i * 12))
                        .ToList();
                
                CategoryComponents =
                    Enumerable.Range(0, _chestCategories.Count)
                        .Select(i => (ClickableComponent)_chestCategories[i])
                        .ToList();

                for (var i = 0; i < CategoryComponents.Count; ++i)
                {
                    if (i > 0)
                    {
                        leftItems[i - 1].leftNeighborID = CategoryComponents[i < 4 ? i - 1 : i].myID;
                        CategoryComponents[i - 1].downNeighborID = CategoryComponents[i].myID;
                        CategoryComponents[i].upNeighborID = CategoryComponents[i - 1].myID;
                    }
                    CategoryComponents[i].myID = i + 239865;
                    CategoryComponents[i].rightNeighborID = leftItems[i < 3 ? i : i - 1].myID;
                }
            }
            */
        }
        public override void draw(SpriteBatch b)
        {
            // Background
            if (!Game1.options.showMenuBackground)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
            }

            _itemsToGrabMenu.draw(b);
            _inventory.draw(b);
            chestColorPicker.draw(b);

            // Inventory Icon
            b.Draw(Game1.mouseCursors,
                new Vector2(xPositionOnScreen - TileSize, _inventory.yPositionOnScreen + 108),
                new Rectangle(16, 368, 12, 16),
                Color.White,
                4.712389f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f);
            b.Draw(Game1.mouseCursors,
                new Vector2(xPositionOnScreen - TileSize, _inventory.yPositionOnScreen + 76),
                new Rectangle(21, 368, 11, 16),
                Color.White,
                4.712389f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f);
            b.Draw(Game1.mouseCursors,
                new Vector2(xPositionOnScreen - BorderWidth, _inventory.yPositionOnScreen + 48),
                new Rectangle(4, 372, 8, 11),
                Color.White,
                0.0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f);

            //poof?.draw(b, true);

            foreach (var clickableComponent in allClickableComponents.OfType<ClickableTextureComponent>())
            {
                switch (clickableComponent.name)
                {
                    case "trashCan":
                        clickableComponent.draw(b);
                        b.Draw(
                            Game1.mouseCursors,
                            new Vector2(clickableComponent.bounds.X + 60, clickableComponent.bounds.Y + 40),
                            new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10),
                            Color.White,
                            trashCanLidRotation,
                            new Vector2(16f, 10f),
                            Game1.pixelZoom,
                            SpriteEffects.None,
                            0.86f);
                        break;
                    default:
                        if (clickableComponent is ChestCategory chestCategory)
                        {
                            chestCategory.Draw(b, chestCategory.Equals(_itemsToGrabMenu.SelectedCategory));
                        }
                        else
                        {
                            clickableComponent.draw(b);
                        }
                        break;
                }
            }

            if (!(hoveredItem is null))
            {
                // Hover Item
                IClickableMenu.drawToolTip(
                    b,
                    hoveredItem.getDescription(),
                    hoveredItem.DisplayName,
                    hoveredItem,
                    !(heldItem is null));
            }
            else if (!(hoverText is null) && hoverAmount > 0)
            {
                // Hover Text w/Amount
                IClickableMenu.drawToolTip(
                    b,
                    hoverText,
                    "",
                    null,
                    true,
                    moneyAmountToShowAtBottom: hoverAmount);
            }
            else if (!(hoverText is null))
            {
                // Hover Text
                IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
            }

            // Held Item
            heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

            // Game Cursor
            Game1.mouseCursorTransparency = 1f;
            drawMouse(b);
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            chestColorPicker.receiveLeftClick(x, y);
            _customChest.playerChoiceColor.Value = chestColorPicker.getColorFromSelection(chestColorPicker.colorSelection);
            foreach (var clickableComponent in allClickableComponents.Where(c => c.containsPoint(x, y)))
            {
                switch (clickableComponent.name)
                {

                    case "colorPickerToggleButton":
                        Game1.player.showChestColorPicker = !Game1.player.showChestColorPicker;
                        chestColorPicker.visible = Game1.player.showChestColorPicker;
                        Game1.playSound("drumkit6");
                        break;
                    case "okButton":
                        exitThisMenu();
                        Game1.playSound("bigDeSelect");
                        break;
                    case "trashCan":
                        Utility.trashItem(heldItem);
                        heldItem = null;
                        break;
                    case "upArrow":
                        _itemsToGrabMenu.ScrollUp();
                        break;
                    case "downArrow":
                        _itemsToGrabMenu.ScrollDown();
                        break;
                    default:
                        if (clickableComponent is ChestCategory chestCategory)
                        {
                            _itemsToGrabMenu.SelectedCategory = chestCategory;
                        }
                        break;
                }
            }
        }
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {

        }
        public override void receiveScrollWheelAction(int direction)
        {
            if (_itemsToGrabMenu.isWithinBounds(Game1.getOldMouseX(), Game1.getOldMouseY()))
            {
                _itemsToGrabMenu.receiveScrollWheelAction(direction);
            }
        }
        public override void performHoverAction(int x, int y)
        {
            hoveredItem = _inventory.hover(x, y, heldItem);
            hoverText = _inventory.hoverText;
            hoverAmount = 0;
            foreach (var clickableComponent in allClickableComponents.OfType<ClickableTextureComponent>())
            {
                if (!(clickableComponent.hoverText is null) && clickableComponent.containsPoint(x, y))
                {
                    hoverText = clickableComponent.hoverText;
                }

                switch (clickableComponent.name)
                {
                    case "fillStacksButton":
                    case "organizeButton":
                        clickableComponent.scale = clickableComponent.containsPoint(x, y)
                            ? Math.Min(Game1.pixelZoom * 1.1f, clickableComponent.scale + 0.05f)
                            : Math.Max(Game1.pixelZoom, clickableComponent.scale - 0.05f);
                        break;
                    case "okButton":
                        clickableComponent.scale = clickableComponent.containsPoint(x, y)
                            ? Math.Min(1.1f, clickableComponent.scale + 0.05f)
                            : Math.Max(1f, clickableComponent.scale - 0.05f);
                        break;
                    case "trashCan":
                        if (clickableComponent.containsPoint(x, y))
                        {
                            if (trashCanLidRotation <= 0f)
                            {
                                Game1.playSound("trashcanlid");
                            }

                            trashCanLidRotation = Math.Min(trashCanLidRotation + (float)Math.PI / 48f, 1.570796f);

                            if (!(heldItem is null) && Utility.getTrashReclamationPrice(heldItem, Game1.player) > 0)
                            {
                                hoverText = Game1.content.LoadString("Strings\\UI:TrashCanSale");
                                hoverAmount = Utility.getTrashReclamationPrice(heldItem, Game1.player);
                            }
                        }
                        else
                        {
                            trashCanLidRotation = Math.Max(trashCanLidRotation - (float)Math.PI / 48f, 0.0f);
                        }
                        break;
                }
            }
        }
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            initialize(
                (Game1.viewport.Width - MenuWidth) / 2 - BorderWidth,
                (Game1.viewport.Height - MenuHeight) / 2 - BorderWidth,
                MenuWidth + BorderWidth * 2,
                MenuHeight + BorderWidth * 2);
            if (yPositionOnScreen < BorderWidth + SpaceToClearTopBorder)
            {
                yPositionOnScreen = BorderWidth + SpaceToClearTopBorder;
            }
            if (xPositionOnScreen < 0)
            {
                xPositionOnScreen = 0;
            }
            _itemsToGrabMenu.xPositionOnScreen = xPositionOnScreen + BorderWidth;
            _itemsToGrabMenu.yPositionOnScreen = yPositionOnScreen;
            _inventory.xPositionOnScreen = xPositionOnScreen + BorderWidth;
            _inventory.yPositionOnScreen = yPositionOnScreen + (TileSize + 4) * Rows + BorderWidth + SpaceToClearSideBorder;
            okButton.bounds.X = xPositionOnScreen + width + SpaceToClearSideBorder;
            okButton.bounds.Y = _inventory.yPositionOnScreen + 140;
            trashCan.bounds.X = xPositionOnScreen + width + SpaceToClearSideBorder - 4;
            trashCan.bounds.Y = _inventory.yPositionOnScreen + 4;
            chestColorPicker.xPositionOnScreen = xPositionOnScreen;
            chestColorPicker.yPositionOnScreen = yPositionOnScreen - 172;
            colorPickerToggleButton.bounds.X = xPositionOnScreen + width + SpaceToClearSideBorder;
            colorPickerToggleButton.bounds.Y = yPositionOnScreen + _itemsToGrabMenu.height / 4 - 32;
            fillStacksButton.bounds.X = xPositionOnScreen + width + SpaceToClearSideBorder;
            fillStacksButton.bounds.Y = yPositionOnScreen + _itemsToGrabMenu.height * 2 / 4 - 32;
            organizeButton.bounds.X = xPositionOnScreen + width + SpaceToClearSideBorder;
            organizeButton.bounds.Y = yPositionOnScreen + _itemsToGrabMenu.height * 3 / 4 - 32;
            UpArrow.bounds.X = xPositionOnScreen + width + 8;
            UpArrow.bounds.Y = yPositionOnScreen - 24;
            DownArrow.bounds.X = xPositionOnScreen + width + 8;
            DownArrow.bounds.Y = yPositionOnScreen + 356;
            var index = 0;
            foreach (var chestCategory in Categories.Select(category => allClickableComponents
                .OfType<ChestCategory>()
                .First(c => c.name == category.Key)))
            {
                chestCategory.xPosition = xPositionOnScreen - BorderWidth - 24;
                chestCategory.yPosition = yPositionOnScreen + index * 60 - 12;
                index++;
            }
        }

        /*********
        ** Private methods
        *********/
        private void SetupItemsMenu()
        {
            _itemsToGrabMenu = new CustomInventoryMenu(
                xPositionOnScreen + BorderWidth,
                yPositionOnScreen,
                Capacity,
                Rows,
                _customChest)
            {
                height = TileSize * Rows + (Rows - 1) * 4
            };
            ItemsToGrabMenu = _itemsToGrabMenu;

            // Color Picker
            chestColorPicker = new DiscreteColorPicker(
                xPositionOnScreen,
                yPositionOnScreen - 172,
                0,
                new Chest(true));
            chestColorPicker.colorSelection =
                chestColorPicker.getSelectionFromColor(_customChest.playerChoiceColor.Value);
            _customChest.playerChoiceColor.Value = chestColorPicker.getColorFromSelection(chestColorPicker.colorSelection);

            // Color Picker Toggle
            colorPickerToggleButton = new ClickableTextureComponent(
                "colorPickerToggleButton",
                new Rectangle(xPositionOnScreen + width + SpaceToClearSideBorder,
                    yPositionOnScreen + _itemsToGrabMenu.height / 4 - 32, TileSize, TileSize),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(119, 469, 16, 16),
                Game1.pixelZoom)
            {
                hoverText = Game1.content.LoadString("Strings\\UI:Toggle_ColorPicker"),
                myID = 27346,
                downNeighborID = -99998,
                leftNeighborID = 53921,
                region = 15923
            };

            // Stack
            fillStacksButton = new ClickableTextureComponent(
                "fillStacksButton",
                new Rectangle(xPositionOnScreen + width + SpaceToClearSideBorder,
                    yPositionOnScreen + _itemsToGrabMenu.height * 2 / 4 - 32, TileSize, TileSize),
                "",
                Game1.content.LoadString("Strings\\UI:ItemGrab_FillStacks"),
                Game1.mouseCursors,
                new Rectangle(103, 469, 16, 16),
                Game1.pixelZoom)
            {
                myID = 12952,
                upNeighborID = 27346,
                downNeighborID = 106,
                leftNeighborID = 53921,
                region = 15923
            };

            // Organize
            organizeButton = new ClickableTextureComponent(
                "organizeButton",
                new Rectangle(xPositionOnScreen + width + SpaceToClearSideBorder,
                    yPositionOnScreen + _itemsToGrabMenu.height * 3 / 4 - 32, TileSize, TileSize),
                "",
                Game1.content.LoadString("Strings\\UI:ItemGrab_Organize"),
                Game1.mouseCursors,
                new Rectangle(162, 440, 16, 16),
                Game1.pixelZoom)
            {
                myID = 106,
                upNeighborID = 12952,
                downNeighborID = 5948,
                leftNeighborID = 53921,
                region = 15923
            };

            // Up Arrow
            UpArrow = new ClickableTextureComponent(
                "upArrow",
                new Rectangle(xPositionOnScreen + width + 8, yPositionOnScreen - 24, TileSize, TileSize),
                "",
                "",
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12),
                1f)
            {
                myID = 88,
                downNeighborID = 89
            };

            // Down Arrow
            DownArrow = new ClickableTextureComponent(
                "downArrow",
                new Rectangle(xPositionOnScreen + width + 8, yPositionOnScreen + 356, TileSize, TileSize),
                "",
                "",
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11),
                1f)
            {
                myID = 89,
                upNeighborID = 88
            };

            // Categories
            var index = 0;
            foreach (var category in Categories)
            {
                if (!ModConfig.Instance.Categories.TryGetValue(category.Key, out var categoryIds) &&
                    !category.Key.Equals("All", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                switch (category.Key)
                {
                    case "All":
                        allClickableComponents.Add(new AllCategory(
                            category.Key,
                            category.Value,
                            xPositionOnScreen - BorderWidth - 24,
                            yPositionOnScreen + index * 60 - 12));
                        break;
                    case "Misc":
                        allClickableComponents.Add(new MiscCategory(
                            category.Key,
                            category.Value,
                            xPositionOnScreen - BorderWidth - 24,
                            yPositionOnScreen + index * 60 - 12,
                            categoryIds));
                        break;
                    default:
                        allClickableComponents.Add(new ChestCategory(
                            category.Key,
                            category.Value,
                            xPositionOnScreen - BorderWidth - 24,
                            yPositionOnScreen + index * 60 - 12,
                            categoryIds));
                        break;
                }
                index++;
            }
            _itemsToGrabMenu.SelectedCategory = allClickableComponents.OfType<ChestCategory>().First();

            allClickableComponents.Add(colorPickerToggleButton);
            allClickableComponents.Add(fillStacksButton);
            allClickableComponents.Add(organizeButton);
            allClickableComponents.Add(UpArrow);
            allClickableComponents.Add(DownArrow);
        }
        private void SetupInventoryMenu()
        {
            _inventory = new CustomInventoryMenu(
                xPositionOnScreen + BorderWidth,
                yPositionOnScreen + (TileSize + 4) * Rows + BorderWidth + SpaceToClearSideBorder)
            {
                height = TileSize * 3 + 8,
                showGrayedOutSlots = true
            };
            inventory = _inventory;

            // OK Button
            okButton = new ClickableTextureComponent(
                "okButton",
                new Rectangle(xPositionOnScreen + width + SpaceToClearSideBorder, _inventory.yPositionOnScreen + 140,
                    TileSize, TileSize),
                "",
                "",
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f)
            {
                myID = 4857,
                upNeighborID = 5948,
                leftNeighborID = 11
            };

            // Trash Can
            trashCan = new ClickableTextureComponent(
                "trashCan",
                new Rectangle(xPositionOnScreen + width + SpaceToClearSideBorder - 4, _inventory.yPositionOnScreen + 4,
                    TileSize, 104),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(564 + Game1.player.trashCanLevel * 18, 102, 18, 26),
                Game1.pixelZoom)
            {
                myID = 106,
                downNeighborID = 4857,
                leftNeighborID = 11,
                upNeighborID = 106
            };

            // Add Invisible Drop Item Button?

            allClickableComponents.Add(okButton);
            allClickableComponents.Add(trashCan);
        }
        private static TemporaryAnimatedSprite CreatePoof(int x, int y) => new TemporaryAnimatedSprite(
            "TileSheets/animations",
            new Rectangle(0, 320, TileSize, TileSize),
            50f,
            8,
            0,
            new Vector2(x - x % TileSize + 16, y - y % TileSize + 16),
            false,
            false);
    }
}