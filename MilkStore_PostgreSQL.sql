-- Xoa du lieu cu
DELETE FROM "Reviews";
DELETE FROM "OrderItems";
DELETE FROM "CartItems";
DELETE FROM "Orders";
DELETE FROM "Products";

-- Reset sequences
ALTER SEQUENCE "Products_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Orders_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "OrderItems_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Reviews_Id_seq" RESTART WITH 1;

-- PRODUCTS
INSERT INTO "Products" ("CategoryId", "BrandId", "ProductName", "Description", "Price", "StockQuantity", "ImageUrl", "ExpiryDate") VALUES
(1,1,'Vinamilk 1L Có Đường','Sữa tươi tiệt trùng Vinamilk được làm từ 100% sữa bò tươi nguyên chất, giàu canxi và protein',32000,120,'https://cdn.tgdd.vn/Products/Images/2386/194408/bhx/thung-12-hop-sua-tuoi-it-duong-vinamilk-100-sua-tuoi-1-lit-202404021115325470.jpg','2025-12-31'),
(1,1,'Vinamilk 1L Không Đường','Sữa tươi',32000,100,'https://fujimart.vn/wp-content/uploads/2024/03/Sua-tuoi-tiet-trung-Vinamilk-khong-duong-1L.png','2025-12-31'),
(1,1,'Vinamilk 180ml Có Đường','Hộp nhỏ',8000,200,'https://cdnv2.tgdd.vn/bhx-static/bhx/production/2025/12/image/Products/Images/2386/85844/bhx/thung-48-hop-sua-tuoi-tiet-trung-vinamilk-100-sua-tuoi-co-duong-180ml_202512221131113240.jpg','2025-12-31'),
(1,1,'Vinamilk 180ml Không Đường','Sữa tươi tiệt trùng không đường Vinamilk 4*180ml(>1Tuổi',8000,180,'https://cdn-v2.kidsplaza.vn/media/catalog/product/s/u/sua-tuoi-tiet-trung-khong-duong-vnm-180ml.jpg','2025-12-31'),
(1,1,'Vinamilk Socola 1L','Sữa tươi tiệt trùng Vinamilk hương Socola - Hộp 180ml',35000,90,'https://bizweb.dktcdn.net/thumb/grande/100/514/431/products/loc-4-hop-sua-tuoi-tiet-trung-huong-socola-vinamilk-100-sua-tuoi-180ml-202310071805525156.jpg','2025-12-31'),
(1,1,'Vinamilk Dâu 110ml','Sữa tươi Vinamilk tiệt trùng hương dâu hộp 110ml',35000,90,'https://suachobeyeu.vn/application/upload/products/sua-tuoi-tiet-trung-vinamilk-dau-hop-110ml-1.jpg','2025-12-31'),
(1,1,'Vinamilk Ít Đường','Thùng 48 hộp sữa tươi tiệt trùng ít đường Vinamilk 100% Sữa tươi 180ml',33000,85,'https://cdn.tgdd.vn/Products/Images/2386/85530/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-vinamilk-100-sua-tuoi-180ml-202310071419485760.jpg','2025-12-31'),
(1,1,'Vinamilk Tách Béo','Lốc 3 Hộp Vinamilk Sữa Tiệt Trùng Green Farm Cao Đạm Ít Béo 250ml',34000,70,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-mbyljh4l21zi79@resize_w900_nl.webp','2025-12-31'),
(1,1,'Vinamilk Organic','Sữa tươi tiệt trùng Vinamilk 100% Organic không đường - Hộp 1L',40000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lza7a6lpb6htee@resize_w900_nl.webp','2025-12-31'),
(1,1,'Vinamilk Lốc 4','Lốc 4 hộp sữa non pha sẵn Optimum Colos 180ml (từ 1 tuổi)',30000,150,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202602/sudd-optimum-colos-hop-180ml-loc-4-thumb150956.jpg','2025-12-31'),
(1,1,'Vinamilk Lốc 6','COMBO 6 LỐC SỮA TƯƠI TIỆT TRÙNG VINAMILK 100% KHÔNG ĐƯỜNG - LỐC 4 HỘP X 180ML',45000,140,'https://salt.tikicdn.com/cache/750x750/ts/product/7c/28/df/f8debfa22368445a6e32fd7e0224ef63.png.webp','2025-12-31'),
(1,1,'Vinamilk Green Farm Ít Đường 180ml','Thùng 48 hộp sữa tươi tiệt trùng Green Farm rất ít đường 180ml',52000,50,'https://cdn.tgdd.vn/Products/Images/2386/85530/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-vinamilk-100-sua-tuoi-180ml-202310071419485760.jpg','2025-12-31'),
(1,2,'TH 1L Có Đường','Sữa tươi tiệt trùng có đường TH true MILK hộp 1 lít',35000,120,'https://cdn.tgdd.vn/Products/Images/2386/79296/bhx/sua-tuoi-tiet-trung-co-duong-th-true-milk-hop-1-lit-202202221429261050.jpg','2025-12-31'),
(1,2,'TH 1L Không Đường','Sữa tươi tiệt trùng TH true MILK không đường 1 lít (từ 1 tuổi)',35000,100,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-tuoi-tiet-trung-nguyen-chat-khong-duong-th-true-milk-hop-1-lit-thumb151346.jpg','2025-12-31'),
(1,2,'TH 180ml','Thùng 48 hộp sữa tươi TH true MILK ít đường 180ml',9000,200,'https://cdn.tgdd.vn/Products/Images/2386/85853/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-th-true-milk-180ml-202207151050154094.jpg','2025-12-31'),
(1,2,'TH Socola','Hộp sữa TH true MILK vị Socola 180ml',37000,90,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-tiet-trung-th-dau-180mlloc-thumb152859.jpg','2025-12-31'),
(1,2,'TH Dâu','Hộp sữa tươi tiệt trùng TH true MILK có đường hương dâu 180ml',37000,90,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-tiet-trung-th-dau-180mlloc-thumb152859.jpg','2025-12-31'),
(1,2,'TH Organic 500ml','Thùng 12 hộp sữa tươi tiệt trùng TH true MILK Organic 500ml',42000,60,'https://cdn.tgdd.vn/Products/Images/2386/193937/bhx/thung-12-hop-sua-tuoi-tiet-trung-th-true-milk-organic-500ml-202202221135089901.jpg','2025-12-31'),
(1,2,'TH Ít Đường','Hộp sữa tươi tiệt trùng ít đường TH true MILK 180ml',35000,85,'https://cdn.tgdd.vn/Products/Images/2386/85853/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-th-true-milk-180ml-202207151050154094.jpg','2025-12-31'),
(1,2,'TH Lốc 4 Có Đường','Lốc 4 hộp sữa tươi tiệt trùng TH true MILK có đường 180ml',32000,150,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/loc-4-hop-sua-tuoi-tiet-trung-co-duong-th-true-milk-180ml-thumb135157.jpg','2025-12-31'),
(1,2,'TH Lốc 4 Không Đường','Lốc 4 hộp sữa tươi tiệt trùng TH true MILK không đường 180ml',48000,140,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/loc-4-hop-sua-tuoi-tiet-trung-nguyen-chat-khong-duong-th-true-milk-180ml-thumb145826.jpg','2025-12-31'),
(1,2,'TH True Formula 110ml','Lốc 4 hộp sữa pha sẵn TH true Formula 110ml (1-2 tuổi)',55000,50,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202601/sbps-th-true-formula-110ml-loc-4-thumb112754.jpg','2025-12-31'),
(1,2,'TH Tách Béo','TH TRUE MILK TIỆT TRÙNG HILO VỊ TỰ NHIÊN 180ML',36000,70,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-m0okhzvodib181@resize_w900_nl.webp','2025-12-31'),
(1,2,'TH Kids','Hộp sữa tươi tiệt trùng kem vanilla tự nhiên TH true MILK Top Kid Organic 180ml',34000,110,'https://cdn.tgdd.vn/Products/Images/2386/178967/bhx/thung-48-hop-sua-tuoi-tiet-trung-kem-vanilla-tu-nhien-th-true-milk-top-kid-organic-180ml-202202211532066451.jpg','2025-12-31'),
(2,3,'Enfagrow Số 1 400g','Sữa Bột Bầu Enfamama A+ với 360° Brain Plus - Vị Vanilla - 400g',280000,80,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie3ajic7coz00@resize_w900_nl.webp','2026-06-30'),
(2,3,'Enfagrow Số 2 400g','Sữa bột Enfamil A+ Neuropro số 2 400g (6-12 tháng)',290000,80,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202507/sua-bot-enfamil-a-neuropro-vi-nhat-de-uong-400g-thumb102201.jpg','2026-06-30'),
(2,3,'Enfagrow Số 3 900g','Sữa bột Enfagrow A+ NeuroPro 3 C-Sec 800g cho trẻ từ 2-6 tuổi',520000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie08d8yj9c4ae@resize_w900_nl.webp','2026-06-30'),
(2,3,'Enfagrow Số 4 900g','Bộ 2 lon sữa bột Enfagrow A+ Neuropro 4 với DHA giúp phát triển não bộ - 830g/lon',530000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie3ao84k64ndd@resize_w900_nl.webp','2026-06-30'),
(2,3,'Enfagrow DHA+','Sữa ENFAGROW 4 A+ MFGM & DHA 1.7Kg (2-6 tuổi)',550000,50,'https://suabottot.com/wp-content/uploads/2022/01/sua-enfagrow-4-1.7kg.jpg','2026-06-30'),
(2,3,'Enfagrow A+','Bộ 2 lon sữa bột Enfagrow A+ Neuropro 4 với DHA - 1.7kg/lon',560000,50,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie3ao84k64ndd@resize_w900_nl.webp','2026-06-30'),
(2,3,'Enfagrow Kids','Sữa Enfagrow A+ 3 400g phát triển não bộ cho bé từ 1-3 tuổi',500000,70,'https://cdn-v2.kidsplaza.vn/media/catalog/product/s/u/sua-bot-enfagrow-a-360-brain-dha-so-3-400g_2_.jpg','2026-06-30'),
(2,3,'Sữa bột Nutifood GrowPLUS','Sữa bột Nutifood GrowPLUS+ Suy Dinh Dưỡng (Đỏ) 1+ - Tăng Cân, Tăng Chiều Cao (Lon 1,65 Kg)',600000,40,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mfh0cthobitqba@resize_w900_nl.webp','2026-06-30'),
(2,3,'Sữa bột Ensure','Sữa Ensure Úc 850G Chính Hãng',650000,30,'https://suabottot.com/wp-content/uploads/2022/01/sua-ensure-uc.jpg','2026-06-30'),
(2,3,'Sữa bột Optimum Gold 850g','Sữa bột Optimum Gold số 3 hương vani 850g (1-2 tuổi)',300000,90,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-bot-optimum-gold-3-lon-850g-1-2-tuoi-thumb023314.jpg','2026-06-30'),
(2,3,'Sữa bột Abbott Grow Gold 3+','Sữa bột Abbott Grow Gold 3+ hương vani 850g (3-6 tuổi)',580000,50,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sb-abbott-grow-3-lon-850g-thumb020033.jpg','2026-06-30'),
(2,3,'Sữa bột Blackmores NewBorn','Sữa bột Blackmores NewBorn Formula số 1 hương vani 900g (0-6 tháng)',620000,40,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-bot-blackmores-newborn-formula-so-1-900g-0-6-thang-thumb000753.jpg','2026-06-30'),
(2,3,'Sữa bột Hikid hương vani','Sữa bột Hikid hương vani (sữa non) 600g (1-9 tuổi)',570000,45,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-hikid-vi-vani-tu-1-9-tuoi-hop-600g-thumb022054.jpg','2026-06-30'),
(3,1,'Sữa chua Vinamilk Dâu','Sữa chua Vinamilk lên men tự nhiên từ 12 triệu men Bulgaricus Châu Âu giúp khỏe tiêu hóa và đề kháng.',12000,200,'https://d8um25gjecm9v.cloudfront.net/cms/SCA_VNM_Dau_1_339bd0a1c6_989c166c0b.png','2025-10-31'),
(3,1,'Sữa chua Vinamilk Nha Đam','Sữa Chua Nha Đam 100gr - Vinamilk',12000,200,'https://bizweb.dktcdn.net/thumb/grande/100/563/786/products/unnamed-7.png','2025-10-31'),
(3,1,'Sữa chua Vinamilk Không Đường','Khỏe tiêu hóa, Khỏe đề kháng.',11000,180,'https://d8um25gjecm9v.cloudfront.net/cms/SCA_VNM_KD_1_d2e754098a_7acad6d99a.png','2025-10-31'),
(3,1,'Sữa chua uống Vinamilk','SỮA CHUA UỐNG PROBI CÁC LOẠI - LỐC 5 CHAI 65ML',15000,220,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mg54w740i9e3d6@resize_w900_nl.webp','2025-10-31'),
(3,1,'Sữa chua Probeauty','Combo mix 3 lốc GrowPLUS+ Váng sữa công thức Sữa non Immunel',18000,150,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-md06fmro3iakd2@resize_w900_nl.webp','2025-10-31'),
(3,2,'TH Yogurt Dâu','Thùng 48 hộp sữa chua uống tiệt trùng hương dâu TH True Yogurt 180ml',13000,200,'https://cdn.tgdd.vn/Products/Images/2944/85871/bhx/thung-48-hop-sua-chua-uong-huong-dau-th-true-yogurt-180ml-202202231043311864.jpg','2025-10-31'),
(3,2,'TH Yogurt Không Đường','Lốc sữa chua ăn TH True vị tự nhiên không đường',12000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lv92l7vijvdm83@resize_w900_nl.webp','2025-10-31'),
(3,2,'TH Yogurt Sầu Riêng','THÙNG 48 hộp sữa chua sầu riêng',13000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-miwi4vhshn9db6@resize_w900_nl.webp','2025-10-31'),
(3,2,'TH Yogurt Uống','Sữa Chua Uống Men Sống TH true YOGURT Hương Vị Tự Nhiên 100ml',15000,200,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lkvtykuwp68b63@resize_w900_nl.webp','2025-10-31'),
(3,2,'TH Yogurt Kids','Sữa chua uống tiệt trùng hương dâu TH True Yogurt Top Kid 110ml',14000,170,'https://cdn.tgdd.vn/Products/Images/2944/87042/bhx/thung-48-hop-sua-chua-uong-huong-dau-th-true-yogurt-top-kid-110ml-202202161442430978.jpg','2025-10-31'),
(3,1,'Sữa chua Mix Trái Cây','Combo mix 4 lốc GrowPLUS+ Váng sữa công thức Cao lớn vượt trội',16000,150,'https://down-vn.img.susercontent.com/file/vn-11134201-7ras8-mats0otck7292d@resize_w900_nl.webp','2025-10-31'),
(3,1,'Sữa chua Socola','SỮA CHUA ĂN VINAMILK LOVE YOGURT TRÂN CHÂU ĐƯỜNG ĐEN 100GR',17000,140,'https://down-vn.img.susercontent.com/file/33511830fa1a8857b913a740366fe4a6@resize_w900_nl.webp','2025-10-31'),
(3,1,'Sữa chua Premium','GrowPLUS+ Váng sữa công thức Cao lớn vượt trội Vani - Hộp 55g',20000,120,'https://cdn.hstatic.net/products/200000821091/3_64c4f96ffee343e9bf6f62de5849a1b8_master.png','2025-10-31'),
(4,1,'Sữa đặc Vinamilk Ông Thọ 380g','Sữa đặc có đường Ông Thọ đỏ - Hộp giấy 380g',18000,300,'https://down-vn.img.susercontent.com/file/vn-11134207-7qukw-lj86zznz4nwsd9@resize_w900_nl.webp','2026-12-31'),
(4,1,'Sữa đặc Vinamilk Ông Thọ 1kg','Sửa Đặc Ông Thọ Đỏ Cao Cấp - Hộp giấy 1kg',45000,150,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-mdv9oyvv0sex08@resize_w900_nl.webp','2026-12-31'),
(4,1,'Sữa đặc Vinamilk Ngôi Sao 380g','Sữa Đặc Ngôi Sao Phương Nam Hộp Giấy 380g',19000,250,'https://down-vn.img.susercontent.com/file/267d55637d13cc66f14c4168b34d819e@resize_w900_nl.webp','2026-12-31'),
(4,4,'Sữa đặc Nestlé 380g','Lon sữa đặc Carnation 380g',20000,200,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mhrd427cta0xd6@resize_w900_nl.webp','2026-12-31'),
(4,4,'Sữa đặc Nestlé 170g','Sữa đặc có đường Nestle Úc 170g',48000,120,'https://bizweb.dktcdn.net/thumb/grande/100/374/252/products/395g.jpg','2026-12-31'),
(4,1,'Sữa đặc Hoàn Hảo','Sữa đặc Hoàn Hảo - Hộp 1.27kg',21000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-m0k2xukakrfx1b@resize_w900_nl.webp','2026-12-31'),
(4,1,'Sữa đặc Vinamilk Socola','Ông Thọ Vị Sôcôla',22000,170,'https://d8um25gjecm9v.cloudfront.net/cms/SD_Ong_Tho_SCL_1_6155d41fdf_dd166e1949.png','2026-12-31'),
(4,4,'Sữa đặc Dutch Lady','Túi Sữa Kem đặc có đường Dutch Lady gói 545g/280g',21000,160,'https://down-vn.img.susercontent.com/file/vn-11134201-7ras8-mb0snxs1x6rwfc@resize_w900_nl.webp','2026-12-31'),
(4,1,'Sữa đặc Vinamilk Premium','SỮA ĐẶC CÓ ĐƯỜNG SỮA CAO CẤP ÔNG THỌ (combo 2 hộp)',25000,140,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mgbny15nkqvgcf@resize_w900_nl.webp','2026-12-31'),
(4,4,'Sữa đặc Nestlé Sweetened Condensed','Sữa đặc tuýp Nestle Sweetened Condensed Milk Úc 170g',26000,130,'https://virgosfamily.com/708-large_default/sa-dac-tuyp-nestle-sweetened-condensed-milk-uc-170g.jpg','2026-12-31');

-- Update password theo Update_MilkStore.sql
UPDATE "Users" SET "Password" = '123456' WHERE "Email" = 'admin@milkstore.vn';
UPDATE "Users" SET "Password" = '7788' WHERE "Email" = 'chau.le@gmail.com';

-- Orders
INSERT INTO "Orders" ("UserId", "OrderDate", "TotalAmount", "Status", "PaymentMethod", "ShippingAddress", "Note") VALUES
(2, now() - INTERVAL '30 days', 160000, 'Completed',  'COD',          'Quận Hoàn Kiếm, Hà Nội',  NULL),
(3, now() - INTERVAL '25 days', 285000, 'Completed',  'BankTransfer', 'Quận Cầu Giấy, Hà Nội',   'Giao giờ hành chính'),
(4, now() - INTERVAL '20 days', 520000, 'Completed',  'COD',          'TP. Hồ Chí Minh',           NULL),
(2, now() - INTERVAL '15 days',  74000, 'Completed',  'COD',          'Quận Hoàn Kiếm, Hà Nội',  NULL),
(5, now() - INTERVAL '10 days', 395000, 'Completed',  'BankTransfer', 'Đà Nẵng',                   'Gửi trước 30 phút'),
(3, now() - INTERVAL '7 days',  340000, 'Shipped',    'COD',          'Quận Cầu Giấy, Hà Nội',   NULL),
(6, now() - INTERVAL '5 days',  136000, 'Shipped',    'BankTransfer', 'Hải Phòng',                 NULL),
(4, now() - INTERVAL '3 days',  695000, 'Processing', 'COD',          'TP. Hồ Chí Minh',           'Để trước cửa'),
(2, now() - INTERVAL '1 days',  110000, 'Pending',    'COD',          'Quận Hoàn Kiếm, Hà Nội',  NULL),
(5, now(),                       567000, 'Pending',    'BankTransfer', 'Đà Nẵng',                   NULL);

-- OrderItems
INSERT INTO "OrderItems" ("OrderId", "ProductId", "Quantity", "PriceAtTime") VALUES
(1,  1, 3, 32000), (1,  9, 1, 68000),
(2,  5, 1, 285000),
(3,  6, 1, 520000),
(4,  9, 1, 68000),  (4, 10, 1, 42000),
(5,  8, 1, 395000),
(6,  3, 2, 35000),  (6,  7, 1, 310000),
(7, 12, 4, 18000),  (7, 10, 2, 42000),
(8,  6, 1, 520000), (8,  4, 3, 38000),
(9,  1, 2, 32000),  (9,  9, 1, 68000),
(10, 8, 1, 395000),(10,  3, 4, 35000),(10,  5, 1, 285000);

-- Reviews
INSERT INTO "Reviews" ("UserId", "ProductId", "Rating", "Comment", "CreatedAt") VALUES
(2,  1, 5, 'Sữa rất ngon, con tôi rất thích uống hàng ngày. Giao hàng nhanh!',          now() - INTERVAL '28 days'),
(3,  5, 5, 'Sữa bột chính hãng, con bé nhà tôi ăn tốt hơn sau khi dùng.',               now() - INTERVAL '22 days'),
(4,  6, 4, 'Sản phẩm tốt, giá hơi cao nhưng chất lượng xứng đáng. Sẽ mua lại.',         now() - INTERVAL '18 days'),
(2,  9, 5, 'Sữa chua ngon, vị dâu tự nhiên không quá ngọt. Đóng gói cẩn thận.',         now() - INTERVAL '13 days'),
(5,  8, 4, 'Mẹ tôi uống thấy khỏe hơn, tiêu hóa tốt hơn. Shop tư vấn nhiệt tình.',     now() - INTERVAL '8 days'),
(3,  3, 5, 'TH True Milk ngon hơn nhiều so với các loại khác. Sẽ mua thường xuyên.',    now() - INTERVAL '5 days'),
(6, 12, 3, 'Sữa đặc bình thường, vừa đủ ngọt. Dùng để pha cà phê thì ok.',              now() - INTERVAL '3 days'),
(4, 10, 5, 'Sữa chua uống rất ngon, mua cho cả nhà uống mỗi buổi sáng.',                now() - INTERVAL '1 days');
-- ============================================================
-- SQL: Thêm cột ParentReviewId và IsAdminReply vào bảng Review
-- Chạy script này trên database (SQL Server hoặc PostgreSQL)
-- ============================================================



PostgreSQL:
ALTER TABLE "Reviews" ADD COLUMN "ParentReviewId" INT NULL;
ALTER TABLE "Reviews" ADD COLUMN "IsAdminReply" BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE "Reviews" ADD CONSTRAINT fk_reviews_parent 
FOREIGN KEY ("ParentReviewId") REFERENCES "Reviews"("Id");