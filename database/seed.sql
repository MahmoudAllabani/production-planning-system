-- Minimal seed for required settings

IF NOT EXISTS (SELECT 1 FROM Ayarlar WHERE Ad = 'yonetici_email')
BEGIN
  INSERT INTO Ayarlar (Ad, Deger) VALUES ('yonetici_email', 'demo@demo.com');
END

IF NOT EXISTS (SELECT 1 FROM Ayarlar WHERE Ad = 'yonetici_sifre')
BEGIN
  INSERT INTO Ayarlar (Ad, Deger) VALUES ('yonetici_sifre', '$2a$11$iuUcgey.BuZw8qI9jXbHWe30w/6aF5Yahn7N6PEUNTLrPQBiJnp9i');
END
