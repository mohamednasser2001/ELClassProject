document.addEventListener('DOMContentLoaded', () => {

    const swipers = [
        { selector: '.tutors-swiper', normalSpeed: 0.6, slowSpeed: 0.12 },
        { selector: '.subjects-swiper', normalSpeed: 0.6, slowSpeed: 0.12 },
        { selector: '.systems-swiper', normalSpeed: 0.6, slowSpeed: 0.12 },
    ];

    swipers.forEach(s => {
        const container = document.querySelector(s.selector);
        const slides = Array.from(container.querySelectorAll('.swiper-slide'));

   
        slides.forEach(slide => {
            const clone = slide.cloneNode(true);
            container.querySelector('.swiper-wrapper').appendChild(clone);
        });

        const swiper = new Swiper(s.selector, {
            slidesPerView: 'auto',
            spaceBetween: 24,
            loop: false, 
            allowTouchMove: true
        });

        let currentSpeed = s.normalSpeed;
        let targetSpeed = s.normalSpeed;
        const easing = 0.08;

        function animate() {
            currentSpeed += (targetSpeed - currentSpeed) * easing;

            let translate = swiper.getTranslate() - currentSpeed;
            swiper.setTranslate(translate);

            const totalWidth = container.querySelector('.swiper-wrapper').scrollWidth / 2;
            if (-translate >= totalWidth) {
                swiper.setTranslate(0);
            }

            swiper.updateProgress();
            swiper.updateSlidesClasses();

            requestAnimationFrame(animate);
        }

        animate();

        container.querySelectorAll('.swiper-slide').forEach(slide => {
            slide.addEventListener('mouseenter', () => targetSpeed = s.slowSpeed);
            slide.addEventListener('mouseleave', () => targetSpeed = s.normalSpeed);
        });
    });

});
