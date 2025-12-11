// CYBER EFFECT: Glow when clicking any card
document.addEventListener("DOMContentLoaded", () => {
    const cards = document.querySelectorAll(".campus-card");

    cards.forEach(card => {
        card.addEventListener("mousedown", () => {
            card.style.transform = "scale(0.97)";
        });

        card.addEventListener("mouseup", () => {
            card.style.transform = "scale(1)";
        });

        card.addEventListener("mouseleave", () => {
            card.style.transform = "scale(1)";
        });
    });
});

// Neon pulse effect for buttons
document.addEventListener("DOMContentLoaded", () => {
    const btns = document.querySelectorAll(".btn-cyber");

    btns.forEach(btn => {
        btn.addEventListener("mouseover", () => {
            btn.style.boxShadow = "0 0 14px rgba(59,130,246,0.6)";
        });
        btn.addEventListener("mouseleave", () => {
            btn.style.boxShadow = "none";
        });
    });
});
