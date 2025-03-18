@echo off
echo Creando entorno virtual...
python -m venv GradoSuperior

echo Activando entorno virtual...
call GradoSuperior\Scripts\activate

echo Instalando paquetes desde requirements.txt...
pip install -r requirements.txt
code .

echo.
echo ✅ Entorno listo. Ya puedes empezar a trabajar.
pause
