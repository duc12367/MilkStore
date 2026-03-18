
SELECT  * FROM Products
SELECT name FROM sys.databases
SELECT * FROM Roles

UPDATE Users
SET Password = '123456'
WHERE Email = 'admin@milkstore.vn';

UPDATE Users
SET Password = '7788'
WHERE Email = 'chau.le@gmail.com';


-- XÓA THEO THỨ TỰ ĐÚNG
DELETE FROM OrderItems;
DELETE FROM CartItems;
DELETE FROM Reviews;
DELETE FROM Products;

-- RESET IDENTITY
DBCC CHECKIDENT ('Products', RESEED, 0);
DBCC CHECKIDENT ('Reviews', RESEED, 0);
INSERT INTO Products (CategoryId, BrandId, ProductName, Description, Price, StockQuantity, ImageUrl, ExpiryDate)
VALUES

-- VINAMILK (12)
(1,1,N'Vinamilk 1L Có Đường',N'Sữa tươi tiệt trùng Vinamilk được làm từ 100% sữa bò tươi nguyên chất, giàu canxi và protein',32000,120,'https://cdn.tgdd.vn/Products/Images/2386/194408/bhx/thung-12-hop-sua-tuoi-it-duong-vinamilk-100-sua-tuoi-1-lit-202404021115325470.jpg','2025-12-31'),
(1,1,N'Vinamilk 1L Không Đường',N'Sữa tươi',32000,100,'https://fujimart.vn/wp-content/uploads/2024/03/Sua-tuoi-tiet-trung-Vinamilk-khong-duong-1L.png','2025-12-31'),
(1,1,N'Vinamilk 180ml Có Đường',N'Hộp nhỏ',8000,200,'https://cdnv2.tgdd.vn/bhx-static/bhx/production/2025/12/image/Products/Images/2386/85844/bhx/thung-48-hop-sua-tuoi-tiet-trung-vinamilk-100-sua-tuoi-co-duong-180ml_202512221131113240.jpg','2025-12-31'),
(1,1,N'Vinamilk 180ml Không Đường',N'Sữa tươi tiệt trùng không đường Vinamilk 4*180ml(>1Tuổi',8000,180,'https://cdn-v2.kidsplaza.vn/media/catalog/product/s/u/sua-tuoi-tiet-trung-khong-duong-vnm-180ml.jpg','2025-12-31'),
(1,1,N'Vinamilk Socola 1L',N'Sữa tươi tiệt trùng Vinamilk hương Socola - Hộp 180ml',35000,90,'https://bizweb.dktcdn.net/thumb/grande/100/514/431/products/loc-4-hop-sua-tuoi-tiet-trung-huong-socola-vinamilk-100-sua-tuoi-180ml-202310071805525156.jpg?v=1715920585257','2025-12-31'),
(1,1,N'Vinamilk Dâu 110L',N'Sữa tươi Vinamilk tiệt trùng hương dâu hộp 110m',35000,90,'https://suachobeyeu.vn/application/upload/products/sua-tuoi-tiet-trung-vinamilk-dau-hop-110ml-1.jpg','2025-12-31'),
(1,1,N'Vinamilk Ít Đường',N'Thùng 48 hộp sữa tươi tiệt trùng ít đường Vinamilk 100% Sữa tươi 180ml',33000,85,'https://cdn.tgdd.vn/Products/Images/2386/85530/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-vinamilk-100-sua-tuoi-180ml-202310071419485760.jpg','2025-12-31'),
(1,1,N'Vinamilk Tách Béo',N'Lốc 3 Hộp Vi-na-milk Sữa Tiệt Trùng Green Farm Cao Đạm Ít Béo 250ml',34000,70,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-mbyljh4l21zi79@resize_w900_nl.webp','2025-12-31'),
(1,1,N'Vinamilk Organic',N'Sữa tươi tiệt trùng Vinamilk 100% Organic không đường - Hộp 1L',40000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lza7a6lpb6htee@resize_w900_nl.webp','2025-12-31'),
(1,1,N'Vinamilk Lốc 4',N'Lốc 4 hộp sữa non pha sẵn Optimum Colos 180 ml (từ 1 tuổi) - Giao bao bì ngẫu nhiên',30000,150,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202602/sudd-optimum-colos-hop-180ml-loc-4-thumb150956.jpg','2025-12-31'),
(1,1,N'Vinamilk Lốc 6',N'COMBO 6 LỐC SỮA TƯƠI TIỆT TRÙNG VINAMILK 100% KHÔNG ĐƯỜNG - LỐC 4 HỘP X 180ML',45000,140,'https://salt.tikicdn.com/cache/750x750/ts/product/7c/28/df/f8debfa22368445a6e32fd7e0224ef63.png.webp','2025-12-31'),
(1,1,N'Vinamilk Green farm rất ít đường180ml',N'Thùng 48 hộp sữa tươi tiệt trùng Green farm rất ít đường180ml',52000,50,'https://down-vn.img.susercontent.com/file/vn-11134258-81ztc-mlq6tcrjcc9449','2025-12-31'),

-- TH TRUE MILK 
(1,2,N'TH 1L Có Đường',N'Sữa tươi tiệt trùng có đường TH true MILK hộp 1 lít',35000,120,'https://cdn.tgdd.vn/Products/Images/2386/79296/bhx/sua-tuoi-tiet-trung-co-duong-th-true-milk-hop-1-lit-202202221429261050.jpg','2025-12-31'),
(1,2,N'TH 1L Không Đường',N'Sữa tươi tiệt trùng TH true MILK không đường 1 lít (từ 1 tuổi)',35000,100,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-tuoi-tiet-trung-nguyen-chat-khong-duong-th-true-milk-hop-1-lit-thumb151346.jpg','2025-12-31'),
(1,2,N'TH 180ml',N'Thùng 48 hộp sữa tươi TH true MILK ít đường 180 m',9000,200,'https://down-vn.img.susercontent.com/file/vn-11134258-81ztc-mlq6tcrjcc9449','2025-12-31'),
(1,2,N'TH Socola',N' Hộp sữa TH true MILK vị Socola 180ml',37000,90,'https://down-vn.img.susercontent.com/file/vn-11134258-81ztc-mlq6tcrjcc9449','2025-12-31'),
(1,2,N'TH Dâu',N'Hộp sữa tươi tiệt trùng TH true MILK có đường hương dâu 180 ml (từ 1 tuổi)',37000,90,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-tiet-trung-th-dau-180mlloc-thumb152859.jpg','2025-12-31'),
(1,2,N'Thùng 12 hộp sữa tươi tiệt trùng TH true MILK Organic 500ml',N'Thùng 12 hộp sữa tươi tiệt trùng TH true MILK Organic 500ml',42000,60,'https://cdn.tgdd.vn/Products/Images/2386/193937/bhx/thung-12-hop-sua-tuoi-tiet-trung-th-true-milk-organic-500ml-202202221135089901.jpg','2025-12-31'),
(1,2,N'TH Ít Đường',N'Hộp sữa tươi tiệt trùng ít đường TH true MILK 180ml',35000,85,'https://cdn.tgdd.vn/Products/Images/2386/85853/bhx/thung-48-hop-sua-tuoi-tiet-trung-it-duong-th-true-milk-180ml-202207151050154094.jpg','2025-12-31'),
(1,2,N'TH Lốc 4',N'Lốc 4 hộp sữa tươi tiệt trùng TH true MILK có đường 180 ml (từ 1 tuổi)',32000,150,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/loc-4-hop-sua-tuoi-tiet-trung-co-duong-th-true-milk-180ml-thumb135157.jpg','2025-12-31'),
(1,2,N'Lốc 4 hộp sữa tươi tiệt trùng TH true MILK không đường 180 ml (từ 1 tuổi)',N'Lốc 4 hộp sữa tươi tiệt trùng TH true MILK không đường 180 ml (từ 1 tuổi)',48000,140,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/loc-4-hop-sua-tuoi-tiet-trung-nguyen-chat-khong-duong-th-true-milk-180ml-thumb145826.jpg','2025-12-31'),
(1,2,N'Lốc 4 hộp sữa pha sẵn TH true Formula 110 ml (1 - 2 tuổi) - Giao bao bì ngẫu nhiên',N'Lốc 4 hộp sữa pha sẵn TH true Formula 110 ml (1 - 2 tuổi) - Giao bao bì ngẫu nhiên',55000,50,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202601/sbps-th-true-formula-110ml-loc-4-thumb112754.jpg','2025-12-31'),
(1,2,N'TH Tách Béo','TH TRUE MILK TIỆT TRÙNG HILO VỊ TỰ NHIÊN 180ML - [Chính hãng]- [Date mới]',36000,70,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-m0okhzvodib181@resize_w900_nl.webp','2025-12-31'),
(1,2,N'TH Kids',N'Hộp sữa tươi tiệt trùng kem vanilla tự nhiên TH true MILK Top Kid Organic 180ml',34000,110,'https://cdn.tgdd.vn/Products/Images/2386/178967/bhx/thung-48-hop-sua-tuoi-tiet-trung-kem-vanilla-tu-nhien-th-true-milk-top-kid-organic-180ml-202202211532066451.jpg','2025-12-31'),

-- ENFAGROW 
(2,3,N'Enfagrow Số 1 400g',N'Sữa Bột Bầu Enfamama A+ với 360° Brain Plus - Vị Vanilla - 400g',280000,80,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie3ajic7coz00@resize_w900_nl.webp','2026-06-30'),
(2,3,N'Enfagrow Số 2 400g',N'Sữa bột Enfamil A+ Neuropro số 2 400g (6 - 12 tháng) - Giao bao bì ngẫu nhiên',290000,80,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202507/sua-bot-enfamil-a-neuropro-vi-nhat-de-uong-400g-thumb102201.jpg','2026-06-30'),
(2,3,N'Enfagrow Số 3 900g',N'[Cho cả trẻ sinh mổ] Sữa bột Enfagrow A+ NeuroPro 3 C-Sec 800g cho trẻ từ 2-6 tuổi',520000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie08d8yj9c4ae@resize_w900_nl.webp','2026-06-30'),
(2,3,N'Enfagrow Số 4 900g',N'Bộ 2 lon sữa bột Enfagrow A+ Neuropro 4 với DHA giúp phát triển não bộ cho trẻ từ 2-6 tuổi - 830g/lon',530000,60,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mie3ao84k64ndd@resize_w900_nl.webp','2026-06-30'),
(2,3,N'Enfagrow DHA+',N'Sữa ENFAGROW 4 A+ MFGM & DHA 1.7Kg (2-6 tuổi)',550000,50,'https://suabottot.com/wp-content/uploads/2022/01/sua-enfagrow-4-1.7kg.jpg','2026-06-30'),
(2,3,N'Enfagrow A+',N'Bộ 2 lon sữa bột Enfagrow A+ Neuropro 4 với DHA giúp phát triển não bộ cho trẻ từ 2-6 tuổi - 1.7kg/lon',560000,50,'https://down-vn.img.susercontent.com/file/vn-11134207-81ztc-mlhaldkzv28412','2026-06-30'),
(2,3,N'Enfagrow Kids',N'Sữa Enfagrow A+ 3 400g phát triển não bộ cho bé từ 1-3 tuổi',500000,70,'https://cdn-v2.kidsplaza.vn/media/catalog/product/s/u/sua-bot-enfagrow-a-360-brain-dha-so-3-400g_2_.jpg','2026-06-30'),
(2,3,N'Sữa bột Nutifood GrowPLUS',N'Sữa bột Nutifood GrowPLUS+ Suy Dinh Dưỡng (Đỏ) 1+ - Tăng Cân, Tăng Chiều Cao (Lon 1,65 Kg)',600000,40,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mfh0cthobitqba@resize_w900_nl.webp','2026-06-30'),
(2,3,N'Sữa bột Ensure',N'Sữa Ensure Úc 850G Chính Hãng Giá Cực Rẻ',650000,30,'https://suabottot.com/wp-content/uploads/2022/01/sua-ensure-uc.jpg','2026-06-30'),
(2,3,N'Sữa bột Optimum Gold 850g',N'Sữa bột Optimum Gold số 3 hương vani 850g (1 - 2 tuổi) - Giao bao bì ngẫu nhiên',300000,90,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-bot-optimum-gold-3-lon-850g-1-2-tuoi-thumb023314.jpg','2026-06-30'),
(2,3,N'Sữa bột Abbott Grow Gold 3+',N'Sữa bột Abbott Grow Gold 3+ hương vani 850g (3 - 6 tuổi)',580000,50,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sb-abbott-grow-3-lon-850g-thumb020033.jpg','2026-06-30'),
(2,3,N'Sữa bột Blackmores NewBorn Formula',N'Sữa bột Blackmores NewBorn Formula số 1 hương vani 900g (0 - 6 tháng)',620000,40,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-bot-blackmores-newborn-formula-so-1-900g-0-6-thang-thumb000753.jpg','2026-06-30'),
(2,3,N'Sữa bột Hikid hương vani',N'Sữa bột Hikid hương vani (sữa non) 600g (1 - 9 tuổi)',570000,45,'https://img.tgdd.vn/imgt/ecom/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/pim/cdn/images/202506/sua-hikid-vi-vani-tu-1-9-tuoi-hop-600g-thumb022054.jpg','2026-06-30'),

-- SỮA CHUA 
(3,1,N'Sữa chua Vinamilk Dâu',N'Sữa chua Vinamilk lên men tự nhiên từ 12 triệu men Bulgaricus Châu Âu giúp khỏe tiêu hóa và đề kháng.',12000,200,'https://d8um25gjecm9v.cloudfront.net/cms/SCA_VNM_Dau_1_339bd0a1c6_989c166c0b.png','2025-10-31'),
(3,1,N'Sữa chua Vinamilk Nha Đam',N'Sữa Chua Nha Đam 100gr- Vinamilk',12000,200,'https://bizweb.dktcdn.net/thumb/grande/100/563/786/products/unnamed-7.png?v=1745305368170','2025-10-31'),
(3,1,N'Sữa chua Vinamilk Không Đường',N'Khỏe tiêu hóa, Khỏe đề kháng.',11000,180,'https://d8um25gjecm9v.cloudfront.net/cms/SCA_VNM_KD_1_d2e754098a_7acad6d99a.png','2025-10-31'),
(3,1,N'Sữa chua uống Vinamilk',N'SỮA CHUA UỐNG PROBI CÁC LOẠI - LỐC 5 CHAI 65ML',15000,220,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mg54w740i9e3d6@resize_w900_nl.webp','2025-10-31'),
(3,1,N'Sữa chua Probeauty',N'Combo mix 3 lốc GrowPLUS+ Váng sữa công thức Sữa non Immunel Hỗ trợ tăng đề kháng & Cao lớn vượt trội (55g x 12 hộp)',18000,150,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-md06fmro3iakd2@resize_w900_nl.webp','2025-10-31'),
(3,2,N'TH Yogurt Dâu',N'Thùng 48 hộp sữa chua uống tiệt trùng hương dâu TH True Yogurt 180ml',13000,200,'https://cdn.tgdd.vn/Products/Images/2944/85871/bhx/thung-48-hop-sua-chua-uong-huong-dau-th-true-yogurt-180ml-202202231043311864.jpg','2025-10-31'),
(3,2,N'TH Yogurt Không Đường',N'Lốc sữa chua ăn TH True vị tự nhiên không đường - Chỉ giao hỏa tốc trong HCM',12000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lv92l7vijvdm83@resize_w900_nl.webp','2025-10-31'),
(3,2,N'TH Yogurt Sầu Riêng',N'THÙNG 48 hộp sữa chua sầu riêng',13000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-miwi4vhshn9db6@resize_w900_nl.webp','2025-10-31'),
(3,2,N'TH Yogurt Uống',N'Sữa Chua Uống Men Sống TH true YOGURT Hương VịTự Nhiên 100m',15000,200,'https://down-vn.img.susercontent.com/file/vn-11134207-7r98o-lkvtykuwp68b63@resize_w900_nl.webp','2025-10-31'),
(3,2,N'TH Yogurt Kids',N'Sữa chua uống tiệt trùng hương dâu TH True Yogurt Top Kid 110ml',14000,170,'https://cdn.tgdd.vn/Products/Images/2944/87042/bhx/thung-48-hop-sua-chua-uong-huong-dau-th-true-yogurt-top-kid-110ml-202202161442430978.jpg','2025-10-31'),
(3,1,N'Sữa chua Mix Trái Cây',N'Combo mix 4 lốc GrowPLUS+ Váng sữa công thức Cao lớn vượt trội Vani & Socola (55g x 16 hộp)',16000,150,'https://down-vn.img.susercontent.com/file/vn-11134201-7ras8-mats0otck7292d@resize_w900_nl.webp','2025-10-31'),
(3,1,N'Sữa chua Socola',N'SỮA CHUA ĂN VINAMILK LOVE YOGURT TRÂN CHÂU ĐƯỜNG ĐEN 100GR',17000,140,'https://down-vn.img.susercontent.com/file/33511830fa1a8857b913a740366fe4a6@resize_w900_nl.webp','2025-10-31'),
(3,1,N'Sữa chua Premium',N'GrowPLUS+ Váng sữa công thức Cao lớn vượt trội Vani - Hộp 55g',20000,120,'https://cdn.hstatic.net/products/200000821091/3_64c4f96ffee343e9bf6f62de5849a1b8_master.png','2025-10-31');

INSERT INTO Products (CategoryId, BrandId, ProductName, Description, Price, StockQuantity, ImageUrl, ExpiryDate)
VALUES
(4,1,N'Sữa đặc Vinamilk Ông Thọ 380g',N'Sữa đặc có đường Ông Thọ đỏ - Hộp giấy 380g',18000,300,'https://down-vn.img.susercontent.com/file/vn-11134207-7qukw-lj86zznz4nwsd9@resize_w900_nl.webp','2026-12-31'),
(4,1,N'Sữa đặc Vinamilk Ông Thọ 1kg',N'Sửa Đặc Ông Thọ Đỏ Cao Cấp [ Hộp giấy 1kg ]',45000,150,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-mdv9oyvv0sex08@resize_w900_nl.webp','2026-12-31'),
(4,1,N'Sữa đặc Vinamilk Ngôi Sao 380g',N'Sữa Đặc Ngôi Sao Phương Nam Hộp Giấy 380g',19000,250,'https://down-vn.img.susercontent.com/file/267d55637d13cc66f14c4168b34d819e@resize_w900_nl.webp','2026-12-31'),
(4,4,N'Sữa đặc Nestlé 380g',N'Lon sữa đặc camation 380g date 2026',20000,200,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mhrd427cta0xd6@resize_w900_nl.webp','2026-12-31'),
(4,4,N'Sữa đặc Nestlé 170kg',N'Sữa đặc có đường Nestle Úc 170g',48000,120,'https://bizweb.dktcdn.net/thumb/grande/100/374/252/products/395g.jpg?v=1772455807987','2026-12-31'),
(4,1,N'Sữa đặc Hoàn Hảo',N'Sữa đặc Hoàn Hảo - Hộp 1.27kg',21000,180,'https://down-vn.img.susercontent.com/file/vn-11134207-7ras8-m0k2xukakrfx1b@resize_w900_nl.webp','2026-12-31'),
(4,1,N'Sữa đặc Vinamilk Socola',N'Ông Thọ Vị Sôcôla',22000,170,'https://d8um25gjecm9v.cloudfront.net/cms/SD_Ong_Tho_SCL_1_6155d41fdf_dd166e1949.png','2026-12-31'),
(4,4,N'Sữa đặc Dutch Lady',N'Túi Sữa Kem đặc có đường Dutch Lady gói 545g/280g - Giao nhanh toàn quốc',21000,160,'https://down-vn.img.susercontent.com/file/vn-11134201-7ras8-mb0snxs1x6rwfc@resize_w900_nl.webp','2026-12-31'),
(4,1,N'Sữa đặc Vinamilk Premium',N'SỮA ĐẶC CÓ ĐƯỜNG _SỮA CAO CẤP ÔNG THỌ [ compo 2 hộp)]',25000,140,'https://down-vn.img.susercontent.com/file/vn-11134207-820l4-mgbny15nkqvgcf@resize_w900_nl.webp','2026-12-31'),
(4,4,N'Sữa đặc Nestlé Nestle Sweetened Condensed',N'Sữa đặc tuýp Nestle Sweetened Condensed Milk Úc 170g',26000,130,'https://virgosfamily.com/708-large_default/sa-dac-tuyp-nestle-sweetened-condensed-milk-uc-170g.jpg','2026-12-31');

UPDATE Products
SET 
    ProductName = N'',
    Description = N'Sản phẩm mới cập nhật',
    Price = 25000,
    StockQuantity = 100,
    ExpiryDate = '2027-01-01',
    ImageUrl = ''
WHERE Id = 5;