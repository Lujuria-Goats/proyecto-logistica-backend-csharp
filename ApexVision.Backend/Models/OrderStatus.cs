﻿namespace ApexVision.Backend.Models;

public enum OrderStatus
{
    Pending,      // Pedido creado, sin asignar o asignado pero no iniciado
    InTransit,    // Conductor en camino
    Delivered,    // Entregado
    Completed,    // Completado (con evidencia si es requerida)
    Cancelled     // Cancelado
}
