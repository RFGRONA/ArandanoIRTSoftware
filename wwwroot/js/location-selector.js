document.addEventListener('DOMContentLoaded', function () {
    // Referencias a los elementos del DOM
    const countrySelect = document.getElementById('country-select');
    const departmentSelect = document.getElementById('department-select');
    const citySelect = document.getElementById('city-select');
    const hiddenCityNameInput = document.getElementById('CityName');

    let locationData = [];

    // Carga los datos desde el archivo JSON
    fetch('/data/colombia.json')
        .then(response => response.json())
        .then(data => {
            locationData = data;
            populateDepartments();
            // Intenta pre-seleccionar los valores si estamos en modo "Editar"
            initializeSelectors();
        })
        .catch(error => console.error('Error al cargar los datos de ubicación:', error));

    // Puebla el selector de departamentos
    function populateDepartments() {
        locationData.forEach(data => {
            const option = document.createElement('option');
            option.value = data.departamento;
            option.textContent = data.departamento;
            departmentSelect.appendChild(option);
        });
    }

    // Puebla el selector de ciudades basado en el departamento seleccionado
    function populateCities(selectedDepartment) {
        // Limpia las opciones anteriores
        citySelect.innerHTML = '<option value="">Seleccione una ciudad...</option>';
        citySelect.disabled = true;

        const departmentData = locationData.find(d => d.departamento === selectedDepartment);
        if (departmentData) {
            departmentData.ciudades.forEach(ciudad => {
                const option = document.createElement('option');
                option.value = ciudad;
                option.textContent = ciudad;
                citySelect.appendChild(option);
            });
            citySelect.disabled = false;
        }
    }

    // Actualiza el valor del campo oculto que se envía al servidor
    function updateHiddenInput() {
        const country = countrySelect.value;
        const department = departmentSelect.value;
        const city = citySelect.value;

        if (city && department && country) {
            hiddenCityNameInput.value = `${city},${department},${country}`;
        } else {
            hiddenCityNameInput.value = "";
        }
    }

    // Lógica para pre-seleccionar valores en el modo "Editar"
    function initializeSelectors() {
        const initialValue = hiddenCityNameInput.value;
        if (initialValue) {
            const parts = initialValue.split(',');
            if (parts.length === 3) {
                const [city, department, country] = parts;

                // Selecciona el departamento
                departmentSelect.value = department;
                // Dispara el evento 'change' para poblar las ciudades
                departmentSelect.dispatchEvent(new Event('change'));

                // Usa un pequeño delay para asegurar que las ciudades se hayan cargado
                setTimeout(() => {
                    citySelect.value = city;
                    // Dispara el evento 'change' final para asegurar que todo esté sincronizado
                    citySelect.dispatchEvent(new Event('change'));
                }, 100);
            }
        }
    }

    // Event Listeners
    departmentSelect.addEventListener('change', function () {
        populateCities(this.value);
        updateHiddenInput(); // Actualiza por si el usuario deja la primera ciudad
    });

    citySelect.addEventListener('change', function () {
        updateHiddenInput();
    });
});