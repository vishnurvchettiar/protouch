namespace ClinicApp.Components.Public;

// Public-site service data. This is the single source for the services page, the
// navigation menu, the service detail pages and the booking form.
// (Moved unchanged from ServiceDetail.razor.)
public record ServiceExtra(string Short, string Image, string Image2, string Image3, (string Title, string Text)[] Steps, (string Q, string A)[] Faq, string[] Posters);

public record StatItem(string Value, string Label);
public record TreatmentItem(string Icon, string Name, string Description);
public record ServicePageData(
    string Id,
    string Name,
    string CategoryLabel,
    string KickerColor,
    string Tagline,
    string BannerGradient,
    string HeroImage,
    string OverviewHeading,
    string OverviewBody,
    StatItem[] KeyStats,
    TreatmentItem[] Treatments,
    string[] SubServices,
    string[] Outcomes,
    string[] GalleryImages,
    string[] HeroBadges
);

public static class PublicCatalog
{
    public static readonly Dictionary<string, ServicePageData> Services = new()
    {
        ["weight-loss"] = new ServicePageData(
            Id: "weight-loss",
            Name: "Weight Loss & Slimming",
            CategoryLabel: "SLIMMING & BODY CARE",
            KickerColor: "#ffeb00",
            Tagline: "Inch loss, figure correction, body shaping and skin tightening — without surgery, fasting or side effects.",
            BannerGradient: "linear-gradient(135deg, #1a4d2e 0%, #2d7a44 100%)",
            HeroImage: "/images/clinic-gallery/gallery-49.jpeg",
            OverviewHeading: "Safe, science-backed slimming without the extremes.",
            OverviewBody: "Our weight-loss programme combines European body-care technology with individual physiotherapy assessment. You lose weight at a healthy, sustainable rate with visible inch loss from the very first session — no starvation, no medicine, no surgery.",
            KeyStats: new[]
            {
                new StatItem("4–8 kg", "weight loss per month"),
                new StatItem("2–5 in", "inch loss per session"),
                new StatItem("0", "side effects"),
                new StatItem("1st session", "visible results"),
            },
            Treatments: new[]
            {
                new TreatmentItem("🔁", "LPG Endermologie", "Non-invasive mechanical massage that stimulates fat cells, boosts circulation and firms skin."),
                new TreatmentItem("📡", "RF Therapy", "Radio-frequency energy heats the deep skin layer to break down fat and tighten tissue."),
                new TreatmentItem("🌡️", "Cryotherapy", "Targeted cold application that freezes and eliminates stubborn fat cells permanently."),
                new TreatmentItem("💆", "Body Wraps", "Compression wraps combined with slimming serums to reduce water retention and tone the skin."),
                new TreatmentItem("🔊", "Ultrasound Cavitation", "Low-frequency ultrasound that breaks fat-cell membranes for natural lymphatic elimination."),
                new TreatmentItem("💅", "Body Spa", "Deep relaxation treatment combining detox, exfoliation and nourishing body care."),
            },
            SubServices: new[] { "Figure Correction", "Body Shaping", "Toning", "Skin Tightening", "Breast Correction", "Body Spa", "Inch Loss Program", "Post-Pregnancy Slimming" },
            Outcomes: new[]
            {
                "4–8 kg weight reduction per month",
                "2–5 inches reduction per session/week",
                "Measurable, visible results from the first session",
                "No fasting, no medicine, no surgery, no side effects",
                "Improved skin tone and firmness",
                "Better posture and body confidence",
            },
            GalleryImages: new[]
            {
                "/images/clinic-gallery/gallery-05.jpeg",
                "/images/clinic-gallery/gallery-17.jpeg",
                "/images/clinic-gallery/gallery-20.jpeg",
                "/images/clinic-gallery/gallery-49.jpeg",
                "/images/clinic-gallery/gallery-58.jpeg",
            },
            HeroBadges: new[] { "✓ No Surgery", "✓ No Fasting", "✓ ISO Certified", "✓ Results from Session 1" }
        ),

        ["physiotherapy"] = new ServicePageData(
            Id: "physiotherapy",
            Name: "Physiotherapy & Rehabilitation",
            CategoryLabel: "PHYSIO & REHAB",
            KickerColor: "#ffeb00",
            Tagline: "Expert pain relief and rehabilitation for back, neck, knee, sports injuries and neurological conditions.",
            BannerGradient: "linear-gradient(135deg, #3a1a4d 0%, #5e2d7a 100%)",
            HeroImage: "/images/clinic-gallery/gallery-47.jpeg",
            OverviewHeading: "Targeted therapy for lasting pain relief and recovery.",
            OverviewBody: "Our physiotherapy clinic provides individually designed treatment plans for acute injuries, chronic pain, neurological conditions and post-surgery rehabilitation. Using a combination of electrotherapy, manual therapy and movement re-education, we help you regain strength, mobility and quality of life.",
            KeyStats: new[]
            {
                new StatItem("1st session", "pain relief begins"),
                new StatItem("30+", "conditions treated"),
                new StatItem("100%", "personalised plans"),
                new StatItem("ISO", "9001:2015 certified"),
            },
            Treatments: new[]
            {
                new TreatmentItem("⚡", "Electrotherapy", "Electrical stimulation to reduce pain, speed healing and improve muscle function."),
                new TreatmentItem("🔊", "Ultrasound Therapy", "Deep tissue ultrasound to reduce inflammation and accelerate tissue repair."),
                new TreatmentItem("🔌", "TENS", "Transcutaneous electrical nerve stimulation — blocks pain signals and relaxes muscles."),
                new TreatmentItem("🤲", "Manual Therapy", "Hands-on joint mobilisation, soft tissue release and therapeutic massage."),
                new TreatmentItem("🏃", "Exercise Therapy", "Guided movement and strengthening programmes tailored to your condition."),
                new TreatmentItem("🧠", "Neurological Rehab", "Specialised exercises and stimulation for stroke, paralysis and nerve conditions."),
            },
            SubServices: new[] { "Back Pain", "Neck Pain", "Knee Pain", "Arthritis", "Spondylitis", "Sinusitis", "Paralysis Recovery", "Orthopaedic Rehab", "Neurological Care", "Sports Injury", "Post-Surgery Rehab", "Stress & Strain" },
            Outcomes: new[]
            {
                "Significant pain reduction from the first session",
                "Improved joint mobility and muscle strength",
                "Faster recovery from injury or surgery",
                "Reduced dependency on painkillers",
                "Better posture and movement patterns",
                "Long-term prevention of recurring injuries",
            },
            GalleryImages: new[]
            {
                "/images/clinic-gallery/gallery-47.jpeg",
                "/images/clinic-gallery/gallery-48.jpeg",
                "/images/clinic-gallery/gallery-17.jpeg",
                "/images/clinic-gallery/gallery-14.jpeg",
            },
            HeroBadges: new[] { "✓ All Ages", "✓ Expert Physiotherapist", "✓ Ortho & Neuro", "✓ Sports Injury" }
        ),

        ["yoga"] = new ServicePageData(
            Id: "yoga",
            Name: "Yoga & Holistic Wellness",
            CategoryLabel: "YOGA & LIFESTYLE",
            KickerColor: "#ffeb00",
            Tagline: "Personalised yoga and lifestyle support for managing chronic conditions and restoring hormonal balance.",
            BannerGradient: "linear-gradient(135deg, #4d3a00 0%, #8a6800 100%)",
            HeroImage: "/images/clinic-gallery/gallery-23.jpeg",
            OverviewHeading: "Holistic healing that goes beyond exercise.",
            OverviewBody: "Our yoga and wellness programmes are built around your specific health conditions — not generic routines. Whether you're managing diabetes, blood pressure, thyroid issues or hormonal imbalances, we design a programme that supports your body's natural healing capacity alongside your medical treatment.",
            KeyStats: new[]
            {
                new StatItem("8+", "chronic conditions supported"),
                new StatItem("100%", "personalised plans"),
                new StatItem("Pre & Post", "natal programmes"),
                new StatItem("Holistic", "mind-body approach"),
            },
            Treatments: new[]
            {
                new TreatmentItem("🧘", "Personalised Yoga", "Custom asana sequences designed for your specific health condition and fitness level."),
                new TreatmentItem("🌬️", "Pranayama", "Breathing techniques to regulate the nervous system, reduce stress and improve organ function."),
                new TreatmentItem("🥗", "Nutrition Coaching", "Personalised dietary guidance to support your wellness goals alongside your yoga practice."),
                new TreatmentItem("🌿", "Lifestyle Counselling", "Holistic guidance on sleep, stress management and daily habits for lasting health."),
                new TreatmentItem("🤱", "Pre-Natal Yoga", "Safe pregnancy yoga to ease discomfort, build strength and prepare for birth."),
                new TreatmentItem("👶", "Post-Natal Recovery", "Gentle recovery yoga to rebuild core strength, balance hormones and support new mothers."),
            },
            SubServices: new[] { "Diabetes Wellness", "Blood Pressure", "Thyroid Health", "Cholesterol Management", "Varicose Veins", "Menstrual Wellness", "Pre-Natal Care", "Post-Natal Recovery", "Hormonal Balance", "Stress & Anxiety" },
            Outcomes: new[]
            {
                "Improved blood sugar levels and metabolic health",
                "Regulated blood pressure and cardiovascular health",
                "Better hormonal balance and menstrual health",
                "Reduced stress, anxiety and improved sleep",
                "Stronger, more flexible body across all life stages",
                "Sustained lifestyle improvements with ongoing support",
            },
            GalleryImages: new[]
            {
                "/images/clinic-gallery/gallery-21.jpeg",
                "/images/clinic-gallery/gallery-23.jpeg",
                "/images/clinic-gallery/gallery-53.jpeg",
                "/images/clinic-gallery/gallery-25.jpeg",
            },
            HeroBadges: new[] { "✓ Condition-Specific", "✓ Pre & Post-Natal", "✓ Hormonal Health", "✓ All Ages" }
        ),

        ["specialised"] = new ServicePageData(
            Id: "specialised",
            Name: "Specialised Programs",
            CategoryLabel: "SPECIALISED CARE",
            KickerColor: "#ffeb00",
            Tagline: "Focused, clinician-guided programmes for height increase, sports recovery and specific health goals.",
            BannerGradient: "linear-gradient(135deg, #4d0a0a 0%, #7a1a1a 100%)",
            HeroImage: "/images/clinic-gallery/gallery-20.jpeg",
            OverviewHeading: "Precision programmes for specific, measurable goals.",
            OverviewBody: "Some goals need a dedicated, specialist approach. Our specialised programmes are designed around a single focused objective — whether that's growing taller, recovering from a sports injury, correcting posture, or managing a specific health condition — with a clinician-supervised plan built around you.",
            KeyStats: new[]
            {
                new StatItem("1–3 in", "height increase possible"),
                new StatItem("Up to 23", "years for height program"),
                new StatItem("100%", "clinician supervised"),
                new StatItem("Custom", "plan per individual"),
            },
            Treatments: new[]
            {
                new TreatmentItem("📏", "Height Increase Program", "Structured stretching, decompression and nutrition protocols designed to maximise growth potential in young adults."),
                new TreatmentItem("⚽", "Sports Rehabilitation", "Sport-specific recovery and strengthening programmes for athletes returning after injury."),
                new TreatmentItem("🦴", "Posture Correction", "Assessment and targeted exercises to correct spinal alignment and postural imbalances."),
                new TreatmentItem("🎯", "Custom Wellness Plans", "Bespoke clinician-designed programmes for any specific health goal not covered by other services."),
            },
            SubServices: new[] { "Height Increase (up to 23 yrs)", "Sports Rehabilitation", "Posture Correction", "Pre-Sports Conditioning", "Custom Health Plans" },
            Outcomes: new[]
            {
                "1–3 inches height increase for eligible candidates (up to age 23)",
                "Faster, safer return to sport after injury",
                "Improved posture and spinal alignment",
                "Clinician-supervised progress at every stage",
                "Clear milestone tracking from first session",
            },
            GalleryImages: new[]
            {
                "/images/clinic-gallery/gallery-30.jpeg",
                "/images/clinic-gallery/gallery-37.jpeg",
                "/images/clinic-gallery/gallery-40.jpeg",
                "/images/clinic-gallery/gallery-56.jpeg",
            },
            HeroBadges: new[] { "✓ Clinician Supervised", "✓ Height Program", "✓ Sports Rehab", "✓ Custom Plans" }
        ),
    };

    public static readonly string[] Order = { "weight-loss", "physiotherapy", "yoga", "specialised" };

    // Extra presentation data for the redesigned pages. Photos are existing site images.
    public static readonly Dictionary<string, ServiceExtra> Extras = new()
    {
        ["weight-loss"] = new ServiceExtra(
            "Slimming",
            "/images/homepage/weight-loss.png", "/images/clinic_hero.jpg", "/images/homepage/yoga.png",
            new[]
            {
                ("Assess", "Body measurements, history and goals."),
                ("Plan", "Treatments and diet matched to your routine."),
                ("Treat", "Sessions at the clinic with visible inch loss."),
                ("Track", "Daily weight and meal check-ins in your portal."),
            },
            new[]
            {
                ("Is there any fasting or medicine?", "No. The programme uses body-care technology, physiotherapy assessment and guided nutrition, with no fasting, medicine or surgery."),
                ("How soon will I see results?", "Visible results begin from the first session. Most clients lose 4–8 kg a month, and results vary by individual."),
                ("Do you help after pregnancy?", "Yes. Post-pregnancy slimming is part of the programme."),
            },
            new[] { "/images/clinic-gallery/gallery-06.jpeg", "/images/clinic-gallery/gallery-11.jpeg", "/images/clinic-gallery/gallery-38.jpeg" }),

        ["physiotherapy"] = new ServiceExtra(
            "Physiotherapy",
            "/images/homepage/physiotherapy-session.png", "/images/homepage/knee-leg.png", "/images/homepage/back-spine.png",
            new[]
            {
                ("Examine", "A full assessment of pain, movement and history."),
                ("Plan", "An individual treatment plan for your condition."),
                ("Treat", "Electrotherapy, manual therapy and exercise."),
                ("Recover", "Strength, mobility and prevention of repeat injury."),
            },
            new[]
            {
                ("When does pain relief begin?", "Pain relief begins from the first session, though recovery time varies by condition."),
                ("Which conditions do you treat?", "Back, neck and knee pain, arthritis, spondylitis, sports injuries, neurological conditions and recovery after surgery, among others."),
                ("Is the plan the same for everyone?", "No. Every plan is individually designed after your assessment."),
            },
            new[] { "/images/clinic-gallery/gallery-24.jpeg", "/images/clinic-gallery/gallery-31.jpeg", "/images/clinic-gallery/gallery-53.jpeg" }),

        ["yoga"] = new ServiceExtra(
            "Yoga & Wellness",
            "/images/homepage/yoga.png", "/images/homepage/full-body-wellness.png", "/images/clinic_hero.jpg",
            new[]
            {
                ("Understand", "Your condition, routine and goals."),
                ("Design", "A yoga and lifestyle programme built for you."),
                ("Practise", "Guided sessions with breathing and nutrition support."),
                ("Sustain", "Ongoing support for lasting habits."),
            },
            new[]
            {
                ("Does yoga replace my medicine?", "No. The programme works alongside your medical treatment and does not replace it."),
                ("Do you offer pre and post natal care?", "Yes. Pre-natal yoga and post-natal recovery programmes are available."),
                ("Are the sessions personalised?", "Yes. Programmes are built around your health condition and fitness level."),
            },
            new[] { "/images/clinic-gallery/gallery-13.jpeg", "/images/clinic-gallery/gallery-20.jpeg", "/images/clinic-gallery/gallery-22.jpeg" }),

        ["specialised"] = new ServiceExtra(
            "Specialised",
            "/images/homepage/specialised-care.png", "/images/homepage/physiotherapy-session.png", "/images/homepage/yoga.png",
            new[]
            {
                ("Assess", "Eligibility, goals and a baseline measurement."),
                ("Plan", "A focused programme with clear milestones."),
                ("Train", "Clinician-supervised sessions at the clinic."),
                ("Measure", "Milestone tracking from the first session."),
            },
            new[]
            {
                ("Who is the height programme for?", "It is for eligible candidates up to age 23. Results vary by individual."),
                ("How is progress tracked?", "Milestones are tracked from the first session and reviewed by your clinician."),
                ("Can you build a plan for another goal?", "Yes. Custom health plans are designed around a specific goal."),
            },
            new[] { "/images/clinic-gallery/gallery-17.jpeg", "/images/clinic-gallery/gallery-15.jpeg", "/images/clinic-gallery/gallery-26.jpeg" }),
    };
}
